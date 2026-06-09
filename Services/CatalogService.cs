using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio CRUD para catálogos de Diamonds (DefaultsUtilidad, etc.)
/// </summary>
public class CatalogService
{
    private readonly string _connectionString;
    private readonly ILogger<CatalogService> _logger;

    public CatalogService(string connectionString, ILogger<CatalogService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    // ── DEFAULTS UTILIDAD ────────────────────────────────────────

    public async Task<List<DefaultUtilidad>> ObtenerDefaultsUtilidadAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<DefaultUtilidad>(
            @"SELECT d.IdDefaultUtilidad,
                     d.DefaultUtilidad AS DefaultUtilidadGeneral,
                     d.DefaultUtilidadGemas,
                     d.DefaultUtilidadReloj,
                     d.IdUsuario,
                     u.Nombre AS NombreUsuario,
                     d.FechaCaptura
              FROM DefaultsUtilidad d
              INNER JOIN Usuarios u ON u.IdUsuario = d.IdUsuario
              WHERE d.IdDefaultUtilidad > 0
              ORDER BY d.FechaCaptura DESC"
        )).ToList();
    }

    public async Task<DefaultUtilidad?> ObtenerDefaultUtilidadAsync(int id)
    {
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<DefaultUtilidad>(
            @"SELECT d.IdDefaultUtilidad,
                     d.DefaultUtilidad AS DefaultUtilidadGeneral,
                     d.DefaultUtilidadGemas,
                     d.DefaultUtilidadReloj,
                     d.IdUsuario,
                     u.Nombre AS NombreUsuario,
                     d.FechaCaptura
              FROM DefaultsUtilidad d
              INNER JOIN Usuarios u ON u.IdUsuario = d.IdUsuario
              WHERE d.IdDefaultUtilidad = @Id",
            new { Id = id });
    }

    public async Task<int> CrearDefaultUtilidadAsync(
        decimal utilidad, decimal utilidadGemas, decimal utilidadReloj, int idUsuario)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO DefaultsUtilidad
                (DefaultUtilidad, DefaultUtilidadGemas, DefaultUtilidadReloj, IdUsuario, FechaCaptura)
              OUTPUT INSERTED.IdDefaultUtilidad
              VALUES (@Utilidad, @UtilidadGemas, @UtilidadReloj, @IdUsuario, GETUTCDATE())",
            new
            {
                Utilidad = utilidad,
                UtilidadGemas = utilidadGemas,
                UtilidadReloj = utilidadReloj,
                IdUsuario = idUsuario
            });
    }

    public async Task ActualizarDefaultUtilidadAsync(
        int id, decimal utilidad, decimal utilidadGemas, decimal utilidadReloj, int idUsuario)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            @"UPDATE DefaultsUtilidad
              SET DefaultUtilidad = @Utilidad,
                  DefaultUtilidadGemas = @UtilidadGemas,
                  DefaultUtilidadReloj = @UtilidadReloj,
                  IdUsuario = @IdUsuario
              WHERE IdDefaultUtilidad = @Id",
            new
            {
                Id = id,
                Utilidad = utilidad,
                UtilidadGemas = utilidadGemas,
                UtilidadReloj = utilidadReloj,
                IdUsuario = idUsuario
            });
    }

    public async Task EliminarDefaultUtilidadAsync(int id)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync("DELETE FROM DefaultsUtilidad WHERE IdDefaultUtilidad = @Id", new { Id = id });
    }

    // ══════════════════════════════════════════════
    // DEFAULTS UTILIDAD EXTRA
    // ══════════════════════════════════════════════
    public async Task<List<DefaultUtilidadExtra>> ObtenerDefaultsUtilidadExtraAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<DefaultUtilidadExtra>(@"
            SELECT IdDefaultUtilidadExtra, DefaultUtilidadExtra AS DefaultUtilidadExtra1, IdUsuario, FechaCaptura
            FROM DefaultsUtilidadExtra ORDER BY FechaCaptura DESC")).ToList();
    }

    public async Task<int> CrearDefaultUtilidadExtraAsync(decimal utilidadExtra, int idUsuario)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO DefaultsUtilidadExtra (DefaultUtilidadExtra, IdUsuario, FechaCaptura) OUTPUT INSERTED.IdDefaultUtilidadExtra VALUES (@UtilidadExtra, @IdUsuario, GETUTCDATE())",
            new { UtilidadExtra = utilidadExtra, IdUsuario = idUsuario });
    }

    public async Task ActualizarDefaultUtilidadExtraAsync(int id, decimal utilidadExtra, int idUsuario)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            @"UPDATE DefaultsUtilidadExtra
              SET DefaultUtilidadExtra = @UtilidadExtra, IdUsuario = @IdUsuario, FechaCaptura = GETUTCDATE()
              WHERE IdDefaultUtilidadExtra = @Id",
            new { Id = id, UtilidadExtra = utilidadExtra, IdUsuario = idUsuario });
    }

    public async Task EliminarDefaultUtilidadExtraAsync(int id)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync("DELETE FROM DefaultsUtilidadExtra WHERE IdDefaultUtilidadExtra = @Id", new { Id = id });
    }

    // ══════════════════════════════════════════════
    // UTILIDAD EXTRA POR PRECIO/GRAMO
    // ══════════════════════════════════════════════
    public async Task<List<UtilidadExtraPrecioGramo>> ObtenerUtilidadExtraPrecioGramoAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<UtilidadExtraPrecioGramo>(@"
            SELECT IdUtilidadExtra, PrecioGramoDesde, PrecioGramoHasta, UtilidadExtra, IdUsuario, FechaCaptura
            FROM UtilidadExtra_PrecioGramo ORDER BY PrecioGramoDesde")).ToList();
    }

    public async Task<int> CrearUtilidadExtraPrecioGramoAsync(decimal desde, decimal hasta, decimal utilidad, int idUsuario)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO UtilidadExtra_PrecioGramo (PrecioGramoDesde, PrecioGramoHasta, UtilidadExtra, IdUsuario, FechaCaptura) OUTPUT INSERTED.IdUtilidadExtra VALUES (@Desde, @Hasta, @Utilidad, @IdUsuario, GETDATE())",
            new { Desde = desde, Hasta = hasta, Utilidad = utilidad, IdUsuario = idUsuario });
    }

    public async Task ActualizarUtilidadExtraPrecioGramoAsync(int id, decimal desde, decimal hasta, decimal utilidad)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM DefaultsUtilidad WHERE IdDefaultUtilidad = @Id",
            new { Id = id });
    }
}
