using System.Data;
using ClosedXML.Excel;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio para el registro de existencias (inventario físico).
/// Migrado de frmRegistroExistencias.frm (VB6 legacy).
/// Flujo: escanear código de barras → registrar en InventarioFisico
///   - Si la pieza existe en catálogo (piezas), se marca faltante=0
///   - Si es pieza compuesta (vCompuestas), se registran todos sus componentes
///   - Si no existe en ningún catálogo, se registra en sobrantes
/// Cancelación: mueve registro a inventariofisicocancelado (solo dentro de 24hrs)
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
    /// Obtiene registros de inventario físico con datos de pieza.
    /// </summary>
    public async Task<List<InventarioFisicoItem>> ObtenerRegistrosAsync(
        string? filtro, string? busqueda, int pageSize = 100)
    {
        var where = filtro switch
        {
            "hoy" => "AND inv.FechaCaptura >= CAST(GETUTCDATE() AS DATE)",
            "semana" => "AND inv.FechaCaptura >= DATEADD(DAY, -7, GETUTCDATE())",
            _ => ""
        };

        var searchWhere = "";
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            searchWhere = @"AND (inv.CodigoBarras LIKE '%' + @Busqueda + '%'
                OR p.Descripcion LIKE '%' + @Busqueda + '%'
                OR vc.Descripcion LIKE '%' + @Busqueda + '%')";
        }

        var sql = $@"
            SELECT TOP (@PageSize)
                inv.Id,
                inv.CodigoBarras,
                inv.FechaCaptura,
                inv.FechaUltEdicion,
                inv.IdUsuario,
                COALESCE(p.Descripcion, vc.Descripcion, s.Descripcion) AS Descripcion,
                COALESCE(p.CBTotal, vc.Precio, s.Precio) AS Precio,
                CASE
                    WHEN p.CodigoBarras IS NOT NULL THEN 'Pieza'
                    WHEN vc.CodigoBarras IS NOT NULL THEN 'Compuesta'
                    WHEN s.CodigoBarras IS NOT NULL THEN 'Sobrante'
                    ELSE 'Desconocido'
                END AS Origen
            FROM InventarioFisico inv
            LEFT JOIN piezas p ON p.CodigoBarras = inv.CodigoBarras
            LEFT JOIN vCompuestas vc ON vc.CodigoBarras = inv.CodigoBarras
            LEFT JOIN sobrantes s ON s.CodigoBarras = inv.CodigoBarras
            WHERE 1=1 {where} {searchWhere}
            ORDER BY inv.FechaCaptura DESC";

        using var conn = CreateConnection();
        return (await conn.QueryAsync<InventarioFisicoItem>(sql, new
        {
            PageSize = pageSize,
            Busqueda = busqueda
        })).ToList();
    }

    /// <summary>
    /// Busca info de una pieza por código de barras en piezas, vCompuestas, o sobrantes.
    /// </summary>
    public async Task<PiezaInfo?> BuscarPiezaAsync(string codigoBarras)
    {
        using var conn = CreateConnection();

        // 1. Buscar en piezas
        var pieza = await conn.QueryFirstOrDefaultAsync<PiezaInfo>(
            "SELECT TOP 1 CodigoBarras, Descripcion, CBTotal AS Precio FROM piezas WHERE CodigoBarras = @CB",
            new { CB = codigoBarras });

        if (pieza != null)
            return pieza;

        // 2. Buscar en compuestas
        var compuesta = await conn.QueryFirstOrDefaultAsync<PiezaInfo>(
            "SELECT TOP 1 CodigoBarras, Descripcion, Precio FROM vCompuestas WHERE CodigoBarras = @CB",
            new { CB = codigoBarras });

        if (compuesta != null)
        {
            compuesta.EsCompuesta = true;
            var componentes = await conn.QueryAsync<string>(
                "SELECT CodigoBarras FROM ComponentesCompuestas WHERE CBPadre = @CB",
                new { CB = codigoBarras });
            compuesta.ComponentesCB = componentes.ToList();
            return compuesta;
        }

        return null; // No encontrada → será sobrante
    }

    /// <summary>
    /// Registra una existencia. Replica la lógica de Command1_Click del VB6.
    /// </summary>
    public async Task<RegistroResultado> RegistrarExistenciaAsync(
        string codigoBarras, int idUsuario)
    {
        if (string.IsNullOrWhiteSpace(codigoBarras) || codigoBarras.Length < 6)
        {
            return new RegistroResultado
            {
                Exito = false,
                Mensaje = "Mala lectura — el código de barras debe tener al menos 6 caracteres."
            };
        }

        codigoBarras = codigoBarras.Trim();
        var ahora = DateTime.UtcNow;

        using var conn = CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        try
        {
            // Registrar en InventarioFisico
            await conn.ExecuteAsync(
                @"INSERT INTO InventarioFisico (CodigoBarras, FechaCaptura, FechaUltEdicion, IdUsuario)
                  VALUES (@CB, @Fecha, @Fecha, @Usuario)",
                new { CB = codigoBarras, Fecha = ahora, Usuario = idUsuario },
                tx);

            // Marcar como no faltante
            await conn.ExecuteAsync(
                "UPDATE piezas SET faltante = 0 WHERE CodigoBarras = @CB",
                new { CB = codigoBarras }, tx);

            // Buscar la pieza
            var pieza = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT TOP 1 CodigoBarras, Descripcion, CBTotal AS Precio FROM piezas WHERE CodigoBarras = @CB",
                new { CB = codigoBarras }, tx);

            if (pieza != null)
            {
                tx.Commit();
                return new RegistroResultado
                {
                    Exito = true,
                    Mensaje = "Pieza registrada correctamente.",
                    CodigoBarras = pieza.CodigoBarras,
                    Descripcion = pieza.Descripcion,
                    Precio = pieza.Precio,
                    Tipo = "Pieza"
                };
            }

            // Buscar en compuestas
            var compuesta = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT TOP 1 CodigoBarras, Descripcion, Precio FROM vCompuestas WHERE CodigoBarras = @CB",
                new { CB = codigoBarras }, tx);

            if (compuesta != null)
            {
                // Registrar componentes individuales
                var componentes = await conn.QueryAsync<string>(
                    "SELECT CodigoBarras FROM ComponentesCompuestas WHERE CBPadre = @CB",
                    new { CB = codigoBarras }, tx);

                foreach (var comp in componentes)
                {
                    await conn.ExecuteAsync(
                        @"INSERT INTO InventarioFisico (CodigoBarras, FechaCaptura, FechaUltEdicion, IdUsuario)
                          VALUES (@CB, @Fecha, @Fecha, @Usuario)",
                        new { CB = comp, Fecha = ahora, Usuario = idUsuario }, tx);
                }

                tx.Commit();
                return new RegistroResultado
                {
                    Exito = true,
                    Mensaje = $"Pieza compuesta registrada ({componentes.Count()} componentes).",
                    CodigoBarras = compuesta.CodigoBarras,
                    Descripcion = compuesta.Descripcion,
                    Precio = compuesta.Precio,
                    Tipo = "Compuesta"
                };
            }

            // No existe → registrar como sobrante (requiere descripción del usuario)
            await conn.ExecuteAsync(
                @"IF NOT EXISTS (SELECT 1 FROM sobrantes WHERE CodigoBarras = @CB)
                  INSERT INTO sobrantes (CodigoBarras, IdUsuario, FechaCaptura, FechaUltEdicion)
                  VALUES (@CB, @Usuario, @Fecha, @Fecha)",
                new { CB = codigoBarras, Usuario = idUsuario, Fecha = ahora }, tx);

            tx.Commit();
            return new RegistroResultado
            {
                Exito = true,
                Mensaje = "Pieza no encontrada en catálogo. Registrada como sobrante.",
                CodigoBarras = codigoBarras,
                Tipo = "Sobrante",
                RequiereDescripcion = true
            };
        }
        catch (Exception ex)
        {
            tx.Rollback();
            _logger.LogError(ex, "Error al registrar existencia CB={CodigoBarras}", codigoBarras);
            return new RegistroResultado
            {
                Exito = false,
                Mensaje = $"Error al registrar: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Actualiza descripción y precio de un sobrante.
    /// </summary>
    public async Task ActualizarSobranteAsync(
        string codigoBarras, string? descripcion, decimal? precio)
    {
        var sql = @"UPDATE sobrantes
                    SET Descripcion = COALESCE(@Desc, Descripcion),
                        Precio = COALESCE(@Precio, Precio),
                        FechaUltEdicion = GETUTCDATE()
                    WHERE CodigoBarras = @CB";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new
        {
            CB = codigoBarras,
            Desc = descripcion,
            Precio = precio
        });
    }

    /// <summary>
    /// Cancela un registro de inventario. Archiva en inventariofisicocancelado.
    /// Solo permite cancelar si no han pasado más de 24 horas.
    /// </summary>
    public async Task<RegistroResultado> CancelarRegistroAsync(int registroId, int canceladoPor)
    {
        using var conn = CreateConnection();
        conn.Open();

        // Obtener el registro
        var registro = await conn.QueryFirstOrDefaultAsync<InventarioFisicoItem>(
            "SELECT TOP 1 Id, CodigoBarras, FechaCaptura, FechaUltEdicion, IdUsuario FROM InventarioFisico WHERE Id = @Id",
            new { Id = registroId });

        if (registro == null)
        {
            return new RegistroResultado
            {
                Exito = false,
                Mensaje = "Registro no encontrado."
            };
        }

        // Verificar 24 horas
        var horas = (DateTime.UtcNow - registro.FechaCaptura).TotalHours;
        if (horas > 24)
        {
            return new RegistroResultado
            {
                Exito = false,
                Mensaje = "No se puede cancelar — han pasado más de 24 horas desde el registro."
            };
        }

        using var tx = conn.BeginTransaction();
        try
        {
            // Archivar en cancelados
            await conn.ExecuteAsync(
                @"INSERT INTO inventariofisicocancelado
                    (CodigoBarras, FechaCaptura, FechaUltEdicion, IdUsuario, FechaCancelacion, CanceladoPor)
                  VALUES (@CB, @FC, @FUE, @Usr, GETUTCDATE(), @CancelPor)",
                new
                {
                    CB = registro.CodigoBarras,
                    FC = registro.FechaCaptura,
                    FUE = registro.FechaUltEdicion,
                    Usr = registro.IdUsuario,
                    CancelPor = canceladoPor
                }, tx);

            // Eliminar el original
            await conn.ExecuteAsync(
                "DELETE FROM InventarioFisico WHERE Id = @Id",
                new { Id = registroId }, tx);

            tx.Commit();

            _logger.LogInformation(
                "Registro {Id} cancelado (CB={CB}) por usuario {User}",
                registroId, registro.CodigoBarras, canceladoPor);

            return new RegistroResultado
            {
                Exito = true,
                Mensaje = $"Registro cancelado correctamente (CB: {registro.CodigoBarras}).",
                CodigoBarras = registro.CodigoBarras
            };
        }
        catch (Exception ex)
        {
            tx.Rollback();
            _logger.LogError(ex, "Error al cancelar registro {Id}", registroId);
            return new RegistroResultado
            {
                Exito = false,
                Mensaje = $"Error al cancelar: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Obtiene estadísticas del inventario para el dashboard.
    /// </summary>
    public async Task<InventarioStats> ObtenerEstadisticasAsync()
    {
        var sql = @"
            SELECT TOP 1
                (SELECT COUNT(*) FROM InventarioFisico
                 WHERE FechaCaptura >= CAST(GETUTCDATE() AS DATE)) AS TotalRegistrosHoy,
                (SELECT COUNT(*) FROM InventarioFisico) AS TotalRegistros,
                (SELECT COUNT(*) FROM sobrantes) AS TotalSobrantes,
                (SELECT COUNT(*) FROM inventariofisicocancelado) AS TotalCancelados";

        using var conn = CreateConnection();
        return await conn.QueryFirstAsync<InventarioStats>(sql);
    }

    /// <summary>
    /// Obtiene todos los registros para exportar a Excel (sin límite de paginación).
    /// </summary>
    public async Task<List<InventarioFisicoItem>> ObtenerRegistrosParaExportarAsync(string? filtro)
    {
        var where = filtro switch
        {
            "hoy" => "AND inv.FechaCaptura >= CAST(GETUTCDATE() AS DATE)",
            "semana" => "AND inv.FechaCaptura >= DATEADD(DAY, -7, GETUTCDATE())",
            _ => ""
        };

        var sql = $@"
            SELECT TOP 50000
                inv.Id,
                inv.CodigoBarras,
                inv.FechaCaptura,
                inv.FechaUltEdicion,
                inv.IdUsuario,
                COALESCE(p.Descripcion, vc.Descripcion, s.Descripcion) AS Descripcion,
                COALESCE(p.CBTotal, vc.Precio, s.Precio) AS Precio,
                CASE
                    WHEN p.CodigoBarras IS NOT NULL THEN 'Pieza'
                    WHEN vc.CodigoBarras IS NOT NULL THEN 'Compuesta'
                    WHEN s.CodigoBarras IS NOT NULL THEN 'Sobrante'
                    ELSE 'Desconocido'
                END AS Origen
            FROM InventarioFisico inv
            LEFT JOIN piezas p ON p.CodigoBarras = inv.CodigoBarras
            LEFT JOIN vCompuestas vc ON vc.CodigoBarras = inv.CodigoBarras
            LEFT JOIN sobrantes s ON s.CodigoBarras = inv.CodigoBarras
            WHERE 1=1 {where}
            ORDER BY inv.FechaCaptura DESC";

        using var conn = CreateConnection();
        return (await conn.QueryAsync<InventarioFisicoItem>(sql)).ToList();
    }

    // ══════════════════════════════════════════════
    // METODOS PARA INVENTARIO FISICO (PAGINA INDEX)
    // ══════════════════════════════════════════════

    /// <summary>
    /// Obtiene registros de inventario con busqueda opcional (un parametro).
    /// </summary>
    public async Task<List<RegistroInventario>> ObtenerRegistrosAsync(string? buscar)
    {
        using var conn = CreateConnection();
        var searchWhere = string.IsNullOrWhiteSpace(buscar) ? "" :
            @"AND (inv.CodigoBarras LIKE @Buscar
              OR p.Descripcion LIKE @Buscar
              OR vc.Descripcion LIKE @Buscar)";
        var sql = $@"SELECT TOP 500
                inv.Id, inv.CodigoBarras, inv.FechaCaptura, inv.FechaUltEdicion, inv.IdUsuario,
                COALESCE(p.Descripcion, vc.Descripcion, s.Descripcion) AS Descripcion,
                COALESCE(p.CBTotal, vc.Precio, s.Precio) AS Precio,
                CASE WHEN p.CodigoBarras IS NOT NULL THEN 'Pieza'
                     WHEN vc.CodigoBarras IS NOT NULL THEN 'Compuesta'
                     WHEN s.CodigoBarras IS NOT NULL THEN 'Sobrante'
                     ELSE 'Desconocido' END AS Origen
            FROM InventarioFisico inv
            LEFT JOIN piezas p ON p.CodigoBarras = inv.CodigoBarras
            LEFT JOIN vCompuestas vc ON vc.CodigoBarras = inv.CodigoBarras
            LEFT JOIN sobrantes s ON s.CodigoBarras = inv.CodigoBarras
            WHERE 1=1 {searchWhere}
            ORDER BY inv.FechaCaptura DESC";
        return (await conn.QueryAsync<RegistroInventario>(sql,
            new { Buscar = $"%{buscar}%" })).ToList();
    }

    /// <summary>
    /// Obtiene sobrantes del inventario.
    /// </summary>
    public async Task<List<PiezaSobrante>> ObtenerSobrantesAsync()
    {
        using var conn = CreateConnection();
        var sql = @"SELECT TOP 500
                CodigoBarras, Descripcion, Precio, FechaCaptura, FechaUltEdicion, IdUsuario
            FROM sobrantes
            ORDER BY FechaCaptura DESC";
        return (await conn.QueryAsync<PiezaSobrante>(sql)).ToList();
    }

    /// <summary>
    /// Obtiene piezas faltantes con busqueda opcional.
    /// </summary>
    public async Task<List<PiezaFaltante>> ObtenerFaltantesAsync(string? buscar)
    {
        using var conn = CreateConnection();
        var searchWhere = string.IsNullOrWhiteSpace(buscar) ? "" :
            "AND (p.CodigoBarras LIKE @Buscar OR p.Descripcion LIKE @Buscar)";
        var sql = $@"SELECT TOP 1000
                p.CodigoBarras, p.Descripcion, p.CBTotal AS Precio,
                g.Grupo, pf.Comentario
            FROM piezas p
            LEFT JOIN grupos g ON g.IdGrupo = p.IdGrupo
            LEFT JOIN piezasfaltantes pf ON pf.CodigoBarras = p.CodigoBarras
            WHERE p.faltante = 1 {searchWhere}
            ORDER BY p.CodigoBarras";
        return (await conn.QueryAsync<PiezaFaltante>(sql,
            new { Buscar = $"%{buscar}%" })).ToList();
    }

    /// <summary>
    /// Registra el escaneo de un codigo de barras y devuelve resultado AJAX.
    /// </summary>
    public async Task<EscaneoResult> RegistrarEscaneoAsync(string codigoBarras, int idUsuario)
    {
        var resultado = await RegistrarExistenciaAsync(codigoBarras, idUsuario);
        return new EscaneoResult
        {
            Success = resultado.Exito,
            Message = resultado.Mensaje,
            CodigoBarras = resultado.CodigoBarras,
            Descripcion = resultado.Descripcion,
            Precio = resultado.Precio,
            Tipo = resultado.Tipo,
            RequiereDescripcion = resultado.RequiereDescripcion
        };
    }

    /// <summary>
    /// Registra una pieza sobrante con descripcion y precio.
    /// </summary>
    public async Task<bool> RegistrarSobranteAsync(
        string codigoBarras, string? descripcion, int? precio, int idUsuario)
    {
        try
        {
            using var conn = CreateConnection();
            await conn.ExecuteAsync(
                @"IF NOT EXISTS (SELECT 1 FROM sobrantes WHERE CodigoBarras = @CB)
                    INSERT INTO sobrantes (CodigoBarras, Descripcion, Precio, IdUsuario, FechaCaptura, FechaUltEdicion)
                    VALUES (@CB, @Desc, @Precio, @Usr, GETUTCDATE(), GETUTCDATE())
                  ELSE
                    UPDATE sobrantes SET Descripcion = @Desc, Precio = @Precio, FechaUltEdicion = GETUTCDATE()
                    WHERE CodigoBarras = @CB",
                new { CB = codigoBarras, Desc = descripcion, Precio = precio, Usr = idUsuario });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar sobrante CB={CB}", codigoBarras);
            return false;
        }
    }

    /// <summary>
    /// Inicia un nuevo inventario fisico (limpia registros anteriores).
    /// </summary>
    public async Task<string> IniciarInventarioAsync(int idUsuario)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            @"INSERT INTO inventariofisicocancelado
                (CodigoBarras, FechaCaptura, FechaUltEdicion, IdUsuario, FechaCancelacion, CanceladoPor)
              SELECT CodigoBarras, FechaCaptura, FechaUltEdicion, IdUsuario, GETUTCDATE(), @Usr
              FROM InventarioFisico",
            new { Usr = idUsuario });
        var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM InventarioFisico");
        await conn.ExecuteAsync("DELETE FROM InventarioFisico");
        _logger.LogInformation("Inventario iniciado por usuario {Usr}. {Count} registros archivados.", idUsuario, count);
        return $"Inventario iniciado. {count} registros anteriores archivados.";
    }

    /// <summary>
    /// Elimina un registro de inventario por Id.
    /// </summary>
    public async Task<bool> EliminarRegistroAsync(int id)
    {
        try
        {
            using var conn = CreateConnection();
            await conn.ExecuteAsync("DELETE FROM InventarioFisico WHERE Id = @Id", new { Id = id });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar registro {Id}", id);
            return false;
        }
    }

    /// <summary>
    /// Exporta los registros de inventario como archivo Excel (.xlsx).
    /// </summary>
    public async Task<byte[]> ExportarExcelAsync()
    {
        var registros = await ObtenerRegistrosParaExportarAsync(null);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Inventario Fisico");

        ws.Cell(1, 1).Value = "Codigo";
        ws.Cell(1, 2).Value = "Descripcion";
        ws.Cell(1, 3).Value = "Tipo";
        ws.Cell(1, 4).Value = "Precio";
        ws.Cell(1, 5).Value = "Fecha";
        ws.Cell(1, 6).Value = "Usuario";

        var hdr = ws.Range(1, 1, 1, 6);
        hdr.Style.Font.Bold = true;
        hdr.Style.Fill.BackgroundColor = XLColor.FromHtml("#2d3436");
        hdr.Style.Font.FontColor = XLColor.White;

        for (int i = 0; i < registros.Count; i++)
        {
            var r = registros[i];
            var row = i + 2;
            ws.Cell(row, 1).Value = r.CodigoBarras;
            ws.Cell(row, 2).Value = r.Descripcion ?? "";
            ws.Cell(row, 3).Value = r.Origen ?? "";
            ws.Cell(row, 4).Value = r.Precio ?? 0m;
            ws.Cell(row, 4).Style.NumberFormat.Format = "$#,##0";
            ws.Cell(row, 5).Value = r.FechaCaptura;
            ws.Cell(row, 5).Style.NumberFormat.Format = "dd/MM/yyyy HH:mm";
            ws.Cell(row, 6).Value = r.IdUsuario;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}
