using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// CRUD de divisores para precio de venta.
/// Tabla: Divisores. Precio = Costo / Divisor. Multiplicador = 1 / Divisor.
/// Origen legacy: frmMultiplicadores.frm (VB6)
/// </summary>
public class DivisoresService
{
    private readonly string _connectionString;
    private readonly ILogger<DivisoresService> _logger;

    public DivisoresService(string connectionString, ILogger<DivisoresService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    /// <summary>Obtiene todos los divisores ordenados por IdDivisor</summary>
    public async Task<List<DivisorItem>> ObtenerTodosAsync()
    {
        const string sql = "SELECT TOP 50 IdDivisor, Divisor, Descripcion, IdUsuario, FechaCaptura, FechaUltEdicion FROM Divisores ORDER BY IdDivisor";

        using var conn = CreateConnection();
        var result = await conn.QueryAsync<DivisorItem>(sql);
        _logger.LogInformation("ObtenerTodos: {Count} divisores", result.AsList().Count);
        return result.AsList();
    }

    /// <summary>Obtiene un divisor por Id</summary>
    public async Task<DivisorItem?> ObtenerPorIdAsync(int idDivisor)
    {
        const string sql = "SELECT TOP 1 IdDivisor, Divisor, Descripcion, IdUsuario, FechaCaptura, FechaUltEdicion FROM Divisores WHERE IdDivisor = @IdDivisor";

        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<DivisorItem>(sql, new { IdDivisor = idDivisor });
    }

    /// <summary>Crea un nuevo divisor. Retorna el IdDivisor generado.</summary>
    public async Task<int> CrearAsync(string descripcion, decimal divisor, int idUsuario)
    {
        const string sql = @"
            INSERT INTO Divisores (Divisor, Descripcion, IdUsuario, FechaCaptura, FechaUltEdicion)
            VALUES (@Divisor, @Descripcion, @IdUsuario, GETUTCDATE(), GETUTCDATE());
            SELECT CAST(SCOPE_IDENTITY() AS INT)";

        using var conn = CreateConnection();
        var id = await conn.ExecuteScalarAsync<int>(sql, new
        {
            Divisor = divisor,
            Descripcion = descripcion,
            IdUsuario = idUsuario
        });

        _logger.LogInformation("Divisor creado: Id={Id}, Desc={Desc}, Divisor={Divisor}",
            id, descripcion, divisor);
        return id;
    }

    /// <summary>Actualiza un divisor existente</summary>
    public async Task<bool> ActualizarAsync(int idDivisor, string descripcion, decimal divisor, int idUsuario)
    {
        const string sql = @"
            UPDATE Divisores
            SET Divisor = @Divisor,
                Descripcion = @Descripcion,
                IdUsuario = @IdUsuario,
                FechaUltEdicion = GETUTCDATE()
            WHERE IdDivisor = @IdDivisor";

        using var conn = CreateConnection();
        var rows = await conn.ExecuteAsync(sql, new
        {
            IdDivisor = idDivisor,
            Divisor = divisor,
            Descripcion = descripcion,
            IdUsuario = idUsuario
        });

        _logger.LogInformation("Divisor actualizado: Id={Id}, Rows={Rows}", idDivisor, rows);
        return rows > 0;
    }

    /// <summary>Elimina un divisor por Id</summary>
    public async Task<bool> EliminarAsync(int idDivisor)
    {
        const string sql = "DELETE FROM Divisores WHERE IdDivisor = @IdDivisor";

        using var conn = CreateConnection();
        var rows = await conn.ExecuteAsync(sql, new { IdDivisor = idDivisor });

        _logger.LogInformation("Divisor eliminado: Id={Id}, Rows={Rows}", idDivisor, rows);
        return rows > 0;
    }
}
