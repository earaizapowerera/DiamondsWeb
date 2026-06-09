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
        await conn.ExecuteAsync(
            "DELETE FROM DefaultsUtilidad WHERE IdDefaultUtilidad = @Id",
            new { Id = id });
    }
}
