using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio para gestionar devoluciones a proveedor.
/// Migrado de frmDevoluciones.frm (VB6 legacy).
/// Flujo: registrar devolucion -> backup etiquetas -> sp_devolucion (mueve piezas a bajaspiezas)
/// Revertir: restauradevolucion (mueve bajaspiezas a piezas) -> elimina de devoluciones
/// </summary>
public class DevolucionService
{
    private readonly string _connectionString;
    private readonly ILogger<DevolucionService> _logger;

    public DevolucionService(string connectionString, ILogger<DevolucionService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    /// <summary>
    /// Lista devoluciones desde vDevoluciones con filtros opcionales
    /// </summary>
    public async Task<List<DevolucionItem>> ObtenerDevolucionesAsync(
        string? buscarTexto, string? filtroRemision)
    {
        using var db = CreateConnection();

        var where = "WHERE 1=1";
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(buscarTexto))
        {
            where += @" AND (CodigoBarras LIKE @buscar
                        OR Descripcion LIKE @buscar
                        OR MotivoDevolucion LIKE @buscar
                        OR NombreProveedor LIKE @buscar)";
            parameters.Add("buscar", $"%{buscarTexto}%");
        }

        if (filtroRemision == "pendiente")
        {
            where += " AND (Remision IS NULL OR Remision = '')";
        }
        else if (filtroRemision == "aplicada")
        {
            where += " AND Remision IS NOT NULL AND Remision <> ''";
        }

        var sql = $@"SELECT TOP 50 CodigoBarras, MotivoDevolucion, Descripcion,
                       Peso, CBTotal, CNTotal, Remision, FechaDevolucion,
                       IdUsuario, Proveedor, NombreProveedor
                FROM vDevoluciones
                {where}
                ORDER BY FechaDevolucion DESC";

        var result = await db.QueryAsync<DevolucionItem>(sql, parameters);
        return result.ToList();
    }

    /// <summary>
    /// Obtiene estadisticas para el dashboard
    /// </summary>
    public async Task<DevolucionStats> ObtenerEstadisticasAsync()
    {
        using var db = CreateConnection();

        var sql = @"SELECT
            (SELECT TOP 1 COUNT(*) FROM devoluciones) AS TotalDevoluciones,
            (SELECT TOP 1 COUNT(*) FROM devoluciones WHERE Remision IS NULL OR Remision = '') AS PendientesRemision,
            (SELECT TOP 1 COUNT(*) FROM devoluciones WHERE Remision IS NOT NULL AND Remision <> '') AS ConRemision,
            (SELECT TOP 1 COUNT(*) FROM devoluciones WHERE CAST(FechaDevolucion AS DATE) = CAST(GETUTCDATE() AS DATE)) AS DevolucionesHoy";

        return await db.QuerySingleAsync<DevolucionStats>(sql);
    }

    /// <summary>
    /// Valida que el codigo de barras exista en piezas (no devuelto aun)
    /// </summary>
    public async Task<PiezaInfo?> ValidarPiezaAsync(string codigoBarras)
    {
        using var db = CreateConnection();

        var sql = @"SELECT TOP 1 CodigoBarras, FechaCaptura, Descripcion, Precio
                    FROM piezas
                    WHERE CodigoBarras = @cb";

        return await db.QuerySingleOrDefaultAsync<PiezaInfo>(sql, new { cb = codigoBarras });
    }

    /// <summary>
    /// Registra una devolucion completa:
    /// 1. Backup etiquetas a bajasetiquetas
    /// 2. Insert en devoluciones
    /// 3. Ejecuta sp_devolucion (mueve piezas a bajaspiezas, elimina de piezas)
    /// </summary>
    public async Task<(bool exito, string mensaje)> RegistrarDevolucionAsync(
        string codigoBarras, string motivo, string? remision, int idUsuario)
    {
        using var db = CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();

        try
        {
            // Validar que la pieza existe
            var pieza = await db.QuerySingleOrDefaultAsync<PiezaInfo>(
                "SELECT TOP 1 CodigoBarras, FechaCaptura, Descripcion, Precio FROM piezas WHERE CodigoBarras = @cb",
                new { cb = codigoBarras }, tx);

            if (pieza == null)
            {
                return (false, "Este codigo de barras no existe en piezas. Verifique si ya se devolvio.");
            }

            // Verificar que no exista ya en devoluciones
            var yaDevuelto = await db.ExecuteScalarAsync<int>(
                "SELECT TOP 1 COUNT(*) FROM devoluciones WHERE CodigoBarras = @cb",
                new { cb = codigoBarras }, tx);

            if (yaDevuelto > 0)
            {
                return (false, "Esta pieza ya tiene una devolucion registrada.");
            }

            // 1. Backup etiquetas a bajasetiquetas
            await db.ExecuteAsync(
                "DELETE bajasetiquetas WHERE CodigoBarras = @cb",
                new { cb = codigoBarras }, tx);

            await db.ExecuteAsync(
                @"INSERT INTO bajasetiquetas
                  SELECT e.*, GETUTCDATE(), 1
                  FROM etiquetas e
                  WHERE e.CodigoBarras = @cb",
                new { cb = codigoBarras }, tx);

            // 2. Insert en devoluciones
            await db.ExecuteAsync(
                @"INSERT INTO devoluciones (CodigoBarras, MotivoDevolucion, Remision, FechaDevolucion, IdUsuario)
                  VALUES (@cb, @motivo, @remision, GETUTCDATE(), @usuario)",
                new { cb = codigoBarras, motivo, remision, usuario = idUsuario }, tx);

            // 3. Ejecutar sp_devolucion (mueve piezas a bajaspiezas)
            await db.ExecuteAsync("sp_devolucion", transaction: tx, commandType: CommandType.StoredProcedure);

            tx.Commit();

            _logger.LogInformation("Devolucion registrada: CB={CB}, Motivo={Motivo}, Usuario={User}",
                codigoBarras, motivo, idUsuario);

            return (true, $"Devolucion registrada para pieza {codigoBarras} ({pieza.Descripcion}).");
        }
        catch (Exception ex)
        {
            tx.Rollback();
            _logger.LogError(ex, "Error registrando devolucion para CB={CB}", codigoBarras);
            return (false, $"Error al registrar: {ex.Message}");
        }
    }

    /// <summary>
    /// Aplica remision/nota de credito a multiples devoluciones seleccionadas
    /// </summary>
    public async Task<(bool exito, string mensaje)> AplicarRemisionAsync(
        string remision, List<string> codigosBarras)
    {
        if (string.IsNullOrWhiteSpace(remision))
            return (false, "Debe ingresar un numero de remision o nota de credito.");

        if (!codigosBarras.Any())
            return (false, "Debe seleccionar al menos una devolucion.");

        using var db = CreateConnection();

        try
        {
            var affected = await db.ExecuteAsync(
                "UPDATE devoluciones SET Remision = @remision WHERE CodigoBarras IN @codigos",
                new { remision, codigos = codigosBarras });

            _logger.LogInformation("Remision '{Rem}' aplicada a {Count} devoluciones", remision, affected);

            return (true, $"Remision '{remision}' aplicada a {affected} devolucion(es).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error aplicando remision '{Rem}'", remision);
            return (false, $"Error al aplicar remision: {ex.Message}");
        }
    }

    /// <summary>
    /// Elimina una devolucion y restaura la pieza al inventario:
    /// 1. Ejecuta restauradevolucion SP (mueve bajaspiezas->piezas, bajasetiquetas->etiquetas)
    /// 2. Elimina el registro de devoluciones
    /// </summary>
    public async Task<(bool exito, string mensaje)> EliminarDevolucionAsync(string codigoBarras)
    {
        using var db = CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();

        try
        {
            // Verificar que exista
            var existe = await db.ExecuteScalarAsync<int>(
                "SELECT TOP 1 COUNT(*) FROM devoluciones WHERE CodigoBarras = @cb",
                new { cb = codigoBarras }, tx);

            if (existe == 0)
            {
                return (false, "No se encontro la devolucion para este codigo de barras.");
            }

            // 1. Restaurar pieza (SP mueve bajaspiezas->piezas, bajasetiquetas->etiquetas)
            await db.ExecuteAsync(
                "restauradevolucion @CodigoBarras",
                new { CodigoBarras = codigoBarras }, tx);

            // 2. Eliminar registro de devolucion
            await db.ExecuteAsync(
                "DELETE devoluciones WHERE CodigoBarras = @cb",
                new { cb = codigoBarras }, tx);

            tx.Commit();

            _logger.LogInformation("Devolucion eliminada y pieza restaurada: CB={CB}", codigoBarras);

            return (true, $"Pieza {codigoBarras} restaurada al inventario.");
        }
        catch (Exception ex)
        {
            tx.Rollback();
            _logger.LogError(ex, "Error eliminando devolucion CB={CB}", codigoBarras);
            return (false, $"Error al eliminar: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifica conexion a base de datos
    /// </summary>
    public async Task<string> TestConexionAsync()
    {
        using var db = CreateConnection();
        var result = await db.ExecuteScalarAsync<int>("SELECT TOP 1 1");
        return result == 1 ? "OK" : "ERROR";
    }
}
