using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// CRUD de monedas -- migracion de frmMonedas.frm (VB6).
/// Tabla: Monedas | Vista: vMonedas (JOIN con usuarios para Nombre).
/// </summary>
public class MonedaService
{
    private readonly string _connectionString;
    private readonly ILogger<MonedaService> _logger;

    public MonedaService(string connectionString, ILogger<MonedaService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    /// <summary>
    /// Lista todas las monedas con nombre de usuario (vista vMonedas).
    /// </summary>
    public async Task<List<MonedaDetalle>> ObtenerTodasAsync()
    {
        const string sql = @"
            SELECT TOP 50
                IdMoneda,
                Moneda AS Nombre,
                Extranjera,
                v.IdUsuario,
                v.Nombre AS NombreUsuario,
                v.FechaCaptura
            FROM vMonedas v
            ORDER BY Moneda";

        using var conn = CreateConnection();
        return (await conn.QueryAsync<MonedaDetalle>(sql)).ToList();
    }

    /// <summary>
    /// Obtiene una moneda por su Id.
    /// </summary>
    public async Task<MonedaDetalle?> ObtenerPorIdAsync(int idMoneda)
    {
        const string sql = @"
            SELECT TOP 1
                IdMoneda,
                Moneda AS Nombre,
                Extranjera,
                v.IdUsuario,
                v.Nombre AS NombreUsuario,
                v.FechaCaptura
            FROM vMonedas v
            WHERE IdMoneda = @IdMoneda";

        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<MonedaDetalle>(sql, new { IdMoneda = idMoneda });
    }

    /// <summary>
    /// Crea una moneda nueva.
    /// </summary>
    public async Task<int> CrearAsync(string nombre, bool extranjera, int idUsuario)
    {
        const string sql = @"
            INSERT INTO Monedas (Moneda, Extranjera, IdUsuario, FechaCaptura)
            VALUES (@Nombre, @Extranjera, @IdUsuario, GETUTCDATE());
            SELECT CAST(SCOPE_IDENTITY() AS INT)";

        using var conn = CreateConnection();
        var id = await conn.ExecuteScalarAsync<int>(sql, new
        {
            Nombre = nombre.Trim(),
            Extranjera = extranjera,
            IdUsuario = idUsuario
        });
        _logger.LogInformation("Moneda creada: Id={Id}, Nombre={Nombre}", id, nombre);
        return id;
    }

    /// <summary>
    /// Actualiza nombre y flag extranjera de una moneda existente.
    /// </summary>
    public async Task ActualizarAsync(int idMoneda, string nombre, bool extranjera, int idUsuario)
    {
        const string sql = @"
            UPDATE Monedas
            SET Moneda = @Nombre,
                Extranjera = @Extranjera,
                IdUsuario = @IdUsuario
            WHERE IdMoneda = @IdMoneda";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new
        {
            IdMoneda = idMoneda,
            Nombre = nombre.Trim(),
            Extranjera = extranjera,
            IdUsuario = idUsuario
        });
        _logger.LogInformation("Moneda actualizada: Id={Id}, Nombre={Nombre}", idMoneda, nombre);
    }

    /// <summary>
    /// Elimina una moneda por Id.
    /// </summary>
    public async Task EliminarAsync(int idMoneda)
    {
        const string sql = "DELETE FROM Monedas WHERE IdMoneda = @IdMoneda";

        using var conn = CreateConnection();
        await conn.ExecuteAsync(sql, new { IdMoneda = idMoneda });
        _logger.LogInformation("Moneda eliminada: Id={Id}", idMoneda);
    }
}
