using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// CRUD para Períodos de Inventario Físico.
/// Origen VB6: frmRegistroPeriodos.frm. Tabla: InventariosFisicos.
/// Antes de crear un período, ejecuta sp_mandarafaltantes (igual que el VB6).
/// </summary>
public class PeriodosService
{
    private readonly string _connectionString;
    private readonly ILogger<PeriodosService> _logger;

    public PeriodosService(string connectionString, ILogger<PeriodosService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    /// <summary>
    /// Lista períodos de inventario con nombre de usuario.
    /// Opcionalmente filtra por rango de fechas.
    /// </summary>
    public async Task<List<PeriodoItem>> ListarAsync(string? buscar = null)
    {
        using var db = CreateConnection();

        var sql = @"SELECT TOP 50
                        p.IdPeriodo, p.PeriodoDesde, p.PeriodoHasta,
                        p.FechaCaptura, p.FechaUltEdicion, p.IdUsuario,
                        u.Nombre AS NombreUsuario
                    FROM InventariosFisicos p
                    LEFT JOIN USUARIOS u ON p.IdUsuario = u.IdUsuario
                    WHERE p.IdPeriodo > 0";

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            sql += " AND (u.Nombre LIKE @Buscar OR CONVERT(VARCHAR, p.PeriodoDesde, 103) LIKE @Buscar OR CONVERT(VARCHAR, p.PeriodoHasta, 103) LIKE @Buscar)";
        }

        sql += " ORDER BY p.IdPeriodo DESC";

        var result = await db.QueryAsync<PeriodoItem>(sql, new { Buscar = $"%{buscar}%" });
        return result.ToList();
    }

    /// <summary>
    /// Obtiene un período por su Id.
    /// </summary>
    public async Task<PeriodoItem?> ObtenerPorIdAsync(int idPeriodo)
    {
        using var db = CreateConnection();

        var sql = @"SELECT TOP 1
                        p.IdPeriodo, p.PeriodoDesde, p.PeriodoHasta,
                        p.FechaCaptura, p.FechaUltEdicion, p.IdUsuario,
                        u.Nombre AS NombreUsuario
                    FROM InventariosFisicos p
                    LEFT JOIN USUARIOS u ON p.IdUsuario = u.IdUsuario
                    WHERE p.IdPeriodo = @IdPeriodo";

        return await db.QueryFirstOrDefaultAsync<PeriodoItem>(sql, new { IdPeriodo = idPeriodo });
    }

    /// <summary>
    /// Crea un nuevo período de inventario.
    /// Ejecuta sp_mandarafaltantes antes del insert (comportamiento VB6 original).
    /// </summary>
    public async Task<int> CrearAsync(DateTime periodoDesde, DateTime? periodoHasta, int idUsuario)
    {
        using var db = CreateConnection();
        db.Open();

        // Ejecutar sp_mandarafaltantes antes de crear (igual que el VB6 original)
        try
        {
            await ((SqlConnection)db).ExecuteAsync("EXEC sp_mandarafaltantes", commandTimeout: 120);
            _logger.LogInformation("sp_mandarafaltantes ejecutado antes de crear período");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al ejecutar sp_mandarafaltantes (no bloquea creación)");
        }

        var sql = @"INSERT INTO InventariosFisicos (PeriodoDesde, PeriodoHasta, FechaCaptura, FechaUltEdicion, IdUsuario)
                    VALUES (@PeriodoDesde, @PeriodoHasta, GETUTCDATE(), GETUTCDATE(), @IdUsuario);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

        var id = await ((SqlConnection)db).QuerySingleAsync<int>(sql, new
        {
            PeriodoDesde = periodoDesde,
            PeriodoHasta = periodoHasta,
            IdUsuario = idUsuario
        });

        _logger.LogInformation("Período creado: Id={Id}, Desde={Desde}, Hasta={Hasta}",
            id, periodoDesde, periodoHasta);
        return id;
    }

    /// <summary>
    /// Actualiza un período existente.
    /// </summary>
    public async Task<bool> ActualizarAsync(int idPeriodo, DateTime periodoDesde, DateTime? periodoHasta, int idUsuario)
    {
        using var db = CreateConnection();

        var sql = @"UPDATE InventariosFisicos
                    SET PeriodoDesde = @PeriodoDesde,
                        PeriodoHasta = @PeriodoHasta,
                        FechaUltEdicion = GETUTCDATE(),
                        IdUsuario = @IdUsuario
                    WHERE IdPeriodo = @IdPeriodo";

        var rows = await db.ExecuteAsync(sql, new
        {
            IdPeriodo = idPeriodo,
            PeriodoDesde = periodoDesde,
            PeriodoHasta = periodoHasta,
            IdUsuario = idUsuario
        });

        _logger.LogInformation("Período actualizado: Id={Id}", idPeriodo);
        return rows > 0;
    }

    /// <summary>
    /// Elimina un período por Id.
    /// </summary>
    public async Task<bool> EliminarAsync(int idPeriodo)
    {
        using var db = CreateConnection();

        var sql = "DELETE FROM InventariosFisicos WHERE IdPeriodo = @IdPeriodo";
        var rows = await db.ExecuteAsync(sql, new { IdPeriodo = idPeriodo });
        _logger.LogInformation("Período eliminado: Id={Id}", idPeriodo);
        return rows > 0;
    }

    /// <summary>
    /// Exporta todos los períodos a formato tabular para Excel.
    /// </summary>
    public async Task<List<PeriodoItem>> ExportarAsync()
    {
        using var db = CreateConnection();

        var sql = @"SELECT TOP 50
                        p.IdPeriodo, p.PeriodoDesde, p.PeriodoHasta,
                        p.FechaCaptura, p.FechaUltEdicion, p.IdUsuario,
                        u.Nombre AS NombreUsuario
                    FROM InventariosFisicos p
                    LEFT JOIN USUARIOS u ON p.IdUsuario = u.IdUsuario
                    ORDER BY p.IdPeriodo DESC";

        var result = await db.QueryAsync<PeriodoItem>(sql);
        return result.ToList();
    }
}
