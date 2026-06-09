using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio para inventario fisico de piezas.
/// Migrado de frmInventarioFisico.frm (VB6).
/// Flujo: escanear codigo → registrar en InventarioFisico → marcar Faltante=0 en piezas
/// → auto-registrar componentes de compuestas → detectar sobrantes.
/// </summary>
public class InventarioFisicoService
{
    private readonly string _connectionString;
    private readonly ILogger<InventarioFisicoService> _logger;

    public InventarioFisicoService(string connectionString, ILogger<InventarioFisicoService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    /// <summary>
    /// Registra un escaneo de codigo de barras.
    /// 1) Inserta en InventarioFisico
    /// 2) Si existe en piezas → marca Faltante=0
    /// 3) Si es compuesta → auto-registra todos sus componentes
    /// 4) Si NO existe en piezas → retorna RequiereDatosSobrante=true
    /// </summary>
    public async Task<EscaneoResult> RegistrarEscaneoAsync(string codigoBarras, int userId)
    {
        codigoBarras = codigoBarras.Trim();
        if (string.IsNullOrEmpty(codigoBarras))
            return new EscaneoResult { Success = false, Message = "Codigo de barras vacio" };

        try
        {
            using var conn = CreateConnection();
            conn.Open();

            // Verificar si ya fue escaneada en este inventario
            var yaExiste = await conn.ExecuteScalarAsync<int>(
                "SELECT TOP 1 COUNT(*) FROM InventarioFisico WHERE CodigoBarras = @CB",
                new { CB = codigoBarras });

            if (yaExiste > 0)
            {
                var descDup = await conn.ExecuteScalarAsync<string>(
                    "SELECT TOP 1 Descripcion FROM piezas WHERE CodigoBarras = @CB",
                    new { CB = codigoBarras });
                var stats = await ObtenerEstadisticasInternalAsync(conn);
                return new EscaneoResult
                {
                    Success = true,
                    Message = $"Pieza {codigoBarras} ya fue escaneada anteriormente",
                    YaEscaneada = true,
                    TipoRegistro = "Duplicada",
                    Descripcion = descDup,
                    Stats = stats
                };
            }

            var ahora = DateTime.UtcNow;

            // Insertar en InventarioFisico
            await conn.ExecuteAsync(
                @"INSERT INTO InventarioFisico (CodigoBarras, FechaCaptura, FechaUltEdicion, IdUsuario)
                  VALUES (@CB, @Fecha, @Fecha, @UserId)",
                new { CB = codigoBarras, Fecha = ahora, UserId = userId });

            // Verificar si existe en piezas
            var descripcionPieza = await conn.ExecuteScalarAsync<string>(
                "SELECT TOP 1 Descripcion FROM piezas WHERE CodigoBarras = @CB",
                new { CB = codigoBarras });

            var existeEnPiezas = descripcionPieza != null;

            if (existeEnPiezas)
            {
                // Marcar como contada (Faltante = 0)
                await conn.ExecuteAsync(
                    "UPDATE piezas SET Faltante = 0 WHERE CodigoBarras = @CB",
                    new { CB = codigoBarras });
            }

            // Verificar si es pieza compuesta
            var esCompuesta = await conn.ExecuteScalarAsync<int>(
                "SELECT TOP 1 COUNT(*) FROM Compuestas WHERE CodigoBarras = @CB",
                new { CB = codigoBarras }) > 0;

            var componentesRegistrados = new List<string>();

            if (esCompuesta)
            {
                // Obtener componentes de la compuesta
                var componentes = (await conn.QueryAsync<string>(
                    "SELECT TOP 50 CodigoBarras FROM ComponentesCompuestas WHERE CBPadre = @CB",
                    new { CB = codigoBarras })).ToList();

                foreach (var comp in componentes)
                {
                    // Solo registrar si no fue escaneado antes
                    var compYaExiste = await conn.ExecuteScalarAsync<int>(
                        "SELECT TOP 1 COUNT(*) FROM InventarioFisico WHERE CodigoBarras = @CB",
                        new { CB = comp });

                    if (compYaExiste == 0)
                    {
                        await conn.ExecuteAsync(
                            @"INSERT INTO InventarioFisico (CodigoBarras, FechaCaptura, FechaUltEdicion, IdUsuario)
                              VALUES (@CB, @Fecha, @Fecha, @UserId)",
                            new { CB = comp, Fecha = ahora, UserId = userId });

                        // Marcar componente como contado
                        await conn.ExecuteAsync(
                            "UPDATE piezas SET Faltante = 0 WHERE CodigoBarras = @CB",
                            new { CB = comp });

                        componentesRegistrados.Add(comp);
                    }
                }

                _logger.LogInformation(
                    "Compuesta {CB}: {Count} componentes auto-registrados",
                    codigoBarras, componentesRegistrados.Count);
            }

            // Si no existe en piezas → necesita registrarse como sobrante
            if (!existeEnPiezas)
            {
                _logger.LogInformation("Pieza {CB} no encontrada en sistema, requiere datos sobrante", codigoBarras);

                var statsS = await ObtenerEstadisticasInternalAsync(conn);
                return new EscaneoResult
                {
                    Success = true,
                    Message = $"Pieza {codigoBarras} NO encontrada en sistema. Registrar como sobrante.",
                    TipoRegistro = "Sobrante",
                    RequiereDatosSobrante = true,
                    Stats = statsS
                };
            }

            var tipo = esCompuesta ? "Compuesta" : "Pieza";
            var msg = esCompuesta
                ? $"Compuesta {codigoBarras} registrada con {componentesRegistrados.Count} componentes"
                : $"Pieza {codigoBarras} registrada - {descripcionPieza}";

            var statsF = await ObtenerEstadisticasInternalAsync(conn);
            return new EscaneoResult
            {
                Success = true,
                Message = msg,
                TipoRegistro = tipo,
                Descripcion = descripcionPieza,
                ComponentesRegistrados = componentesRegistrados,
                Stats = statsF
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar escaneo de {CB}", codigoBarras);
            return new EscaneoResult { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Registra datos de una pieza sobrante (no existe en sistema)
    /// </summary>
    public async Task<bool> RegistrarSobranteAsync(
        string codigoBarras, string? descripcion, int? precio, int userId)
    {
        try
        {
            using var conn = CreateConnection();
            var ahora = DateTime.UtcNow;

            // Verificar si ya existe en sobrantes
            var yaExiste = await conn.ExecuteScalarAsync<int>(
                "SELECT TOP 1 COUNT(*) FROM sobrantes WHERE CodigoBarras = @CB",
                new { CB = codigoBarras });

            if (yaExiste > 0)
            {
                await conn.ExecuteAsync(
                    @"UPDATE sobrantes SET Descripcion = @Desc, Precio = @Precio,
                      FechaUltEdicion = @Fecha WHERE CodigoBarras = @CB",
                    new { CB = codigoBarras, Desc = descripcion, Precio = precio, Fecha = ahora });
            }
            else
            {
                await conn.ExecuteAsync(
                    @"INSERT INTO sobrantes (CodigoBarras, Descripcion, Precio, FechaCaptura, FechaUltEdicion, IdUsuario)
                      VALUES (@CB, @Desc, @Precio, @Fecha, @Fecha, @UserId)",
                    new { CB = codigoBarras, Desc = descripcion, Precio = precio, Fecha = ahora, UserId = userId });
            }

            _logger.LogInformation("Sobrante {CB} registrada: {Desc}, Precio={Precio}",
                codigoBarras, descripcion, precio);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar sobrante {CB}", codigoBarras);
            return false;
        }
    }

    /// <summary>
    /// Obtiene los registros del inventario fisico actual, unido con piezas para descripcion
    /// </summary>
    public async Task<List<RegistroInventario>> ObtenerRegistrosAsync(string? buscar = null)
    {
        var sql = @"
            SELECT TOP 50
                i.Id, i.CodigoBarras, p.Descripcion, i.FechaCaptura, i.IdUsuario,
                CASE
                    WHEN c.CodigoBarras IS NOT NULL THEN 'Compuesta'
                    WHEN p.CodigoBarras IS NULL THEN 'Sobrante'
                    ELSE 'Pieza'
                END AS TipoRegistro,
                cc.CBPadre AS CBPadreCompuesta
            FROM InventarioFisico i
            LEFT JOIN piezas p ON p.CodigoBarras = i.CodigoBarras
            LEFT JOIN Compuestas c ON c.CodigoBarras = i.CodigoBarras
            LEFT JOIN ComponentesCompuestas cc ON cc.CodigoBarras = i.CodigoBarras
            WHERE (@Buscar IS NULL
                OR i.CodigoBarras LIKE '%' + @Buscar + '%'
                OR p.Descripcion LIKE '%' + @Buscar + '%')
            ORDER BY i.FechaCaptura DESC";

        try
        {
            using var conn = CreateConnection();
            return (await conn.QueryAsync<RegistroInventario>(sql, new
            {
                Buscar = string.IsNullOrWhiteSpace(buscar) ? null : buscar
            })).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener registros de inventario");
            throw;
        }
    }

    /// <summary>
    /// Obtiene la lista de sobrantes registrados
    /// </summary>
    public async Task<List<PiezaSobrante>> ObtenerSobrantesAsync()
    {
        var sql = @"SELECT TOP 50 CodigoBarras, Descripcion, Precio, FechaCaptura, IdUsuario
                    FROM sobrantes ORDER BY FechaCaptura DESC";

        using var conn = CreateConnection();
        return (await conn.QueryAsync<PiezaSobrante>(sql)).ToList();
    }

    /// <summary>
    /// Estadisticas del inventario fisico
    /// </summary>
    public async Task<InventarioStats> ObtenerEstadisticasAsync()
    {
        using var conn = CreateConnection();
        conn.Open();
        return await ObtenerEstadisticasInternalAsync(conn);
    }

    private async Task<InventarioStats> ObtenerEstadisticasInternalAsync(IDbConnection conn)
    {
        var sql = @"
            SELECT TOP 1
                (SELECT COUNT(*) FROM InventarioFisico) AS TotalEscaneadas,
                (SELECT COUNT(*) FROM InventarioFisico i
                 INNER JOIN piezas p ON p.CodigoBarras = i.CodigoBarras
                 WHERE NOT EXISTS (SELECT 1 FROM Compuestas c WHERE c.CodigoBarras = i.CodigoBarras)) AS EnSistema,
                (SELECT COUNT(*) FROM InventarioFisico i
                 WHERE NOT EXISTS (SELECT 1 FROM piezas p WHERE p.CodigoBarras = i.CodigoBarras)) AS Sobrantes,
                (SELECT COUNT(*) FROM InventarioFisico i
                 INNER JOIN Compuestas c ON c.CodigoBarras = i.CodigoBarras) AS Compuestas,
                (SELECT COUNT(*) FROM InventarioFisico i
                 INNER JOIN ComponentesCompuestas cc ON cc.CodigoBarras = i.CodigoBarras) AS ComponentesAuto,
                (SELECT COUNT(*) FROM piezas WHERE Faltante = 1) AS Faltantes";

        return await conn.QueryFirstOrDefaultAsync<InventarioStats>(sql) ?? new InventarioStats();
    }

    /// <summary>
    /// Eliminar un registro del inventario (y revertir Faltante si aplica)
    /// </summary>
    public async Task<bool> EliminarRegistroAsync(int id)
    {
        try
        {
            using var conn = CreateConnection();
            conn.Open();

            // Obtener CB antes de borrar
            var cb = await conn.ExecuteScalarAsync<string>(
                "SELECT TOP 1 CodigoBarras FROM InventarioFisico WHERE Id = @Id",
                new { Id = id });

            if (cb == null) return false;

            // Borrar registro
            await conn.ExecuteAsync("DELETE FROM InventarioFisico WHERE Id = @Id", new { Id = id });

            // Si no hay mas escaneos de este CB, revertir Faltante a 1
            var otrosEscaneos = await conn.ExecuteScalarAsync<int>(
                "SELECT TOP 1 COUNT(*) FROM InventarioFisico WHERE CodigoBarras = @CB",
                new { CB = cb });

            if (otrosEscaneos == 0)
            {
                await conn.ExecuteAsync(
                    "UPDATE piezas SET Faltante = 1 WHERE CodigoBarras = @CB",
                    new { CB = cb });
            }

            _logger.LogInformation("Registro {Id} (CB={CB}) eliminado del inventario", id, cb);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar registro {Id}", id);
            return false;
        }
    }

    /// <summary>
    /// Iniciar inventario fisico: marca TODAS las piezas como Faltante=1
    /// y limpia la tabla de inventario y sobrantes previos
    /// </summary>
    public async Task<string> IniciarInventarioAsync(int userId)
    {
        try
        {
            using var conn = CreateConnection();
            conn.Open();

            var totalPiezas = await conn.ExecuteScalarAsync<int>("SELECT TOP 1 COUNT(*) FROM piezas");

            await conn.ExecuteAsync("UPDATE piezas SET Faltante = 1");
            await conn.ExecuteAsync("DELETE FROM InventarioFisico");
            await conn.ExecuteAsync("DELETE FROM sobrantes");

            _logger.LogInformation(
                "Inventario fisico iniciado por usuario {UserId}. {Total} piezas marcadas como faltantes",
                userId, totalPiezas);

            return $"Inventario iniciado. {totalPiezas} piezas marcadas como faltantes. Comience a escanear.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al iniciar inventario fisico");
            throw;
        }
    }

    /// <summary>
    /// Obtiene las piezas faltantes (no contadas en el inventario)
    /// </summary>
    public async Task<List<PiezaFaltante>> ObtenerFaltantesAsync(string? buscar = null)
    {
        var sql = @"
            SELECT TOP 50 CodigoBarras, Descripcion, Precio
            FROM piezas
            WHERE Faltante = 1
              AND (@Buscar IS NULL
                OR CodigoBarras LIKE '%' + @Buscar + '%'
                OR Descripcion LIKE '%' + @Buscar + '%')
            ORDER BY CodigoBarras";

        using var conn = CreateConnection();
        return (await conn.QueryAsync<PiezaFaltante>(sql, new
        {
            Buscar = string.IsNullOrWhiteSpace(buscar) ? null : buscar
        })).ToList();
    }

    /// <summary>
    /// Exporta el inventario fisico actual como datos para Excel (CSV)
    /// </summary>
    public async Task<byte[]> ExportarExcelAsync()
    {
        var sql = @"
            SELECT TOP 5000
                i.CodigoBarras,
                ISNULL(p.Descripcion, s.Descripcion) AS Descripcion,
                CASE
                    WHEN comp.CodigoBarras IS NOT NULL THEN 'Compuesta'
                    WHEN p.CodigoBarras IS NULL THEN 'Sobrante'
                    ELSE 'Pieza'
                END AS Tipo,
                ISNULL(p.Precio, s.Precio) AS Precio,
                i.FechaCaptura
            FROM InventarioFisico i
            LEFT JOIN piezas p ON p.CodigoBarras = i.CodigoBarras
            LEFT JOIN sobrantes s ON s.CodigoBarras = i.CodigoBarras
            LEFT JOIN Compuestas comp ON comp.CodigoBarras = i.CodigoBarras
            ORDER BY i.FechaCaptura";

        using var conn = CreateConnection();
        var registros = await conn.QueryAsync(sql);

        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, System.Text.Encoding.UTF8);

        // BOM para Excel
        await writer.WriteAsync('\uFEFF');
        await writer.WriteLineAsync("CodigoBarras,Descripcion,Tipo,Precio,FechaCaptura");

        foreach (var r in registros)
        {
            var desc = ((string?)r.Descripcion ?? "").Replace("\"", "\"\"");
            var precio = r.Precio?.ToString() ?? "";
            var fecha = ((DateTime)r.FechaCaptura).ToString("dd/MM/yyyy HH:mm:ss");
            await writer.WriteLineAsync($"\"{r.CodigoBarras}\",\"{desc}\",\"{r.Tipo}\",{precio},\"{fecha}\"");
        }

        await writer.FlushAsync();
        return ms.ToArray();
    }
}
