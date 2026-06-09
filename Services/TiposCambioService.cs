using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio CRUD para tipos de cambio por moneda.
/// Tablas: TiposCambio, Monedas. Vista: vTiposCambio.
/// </summary>
public class TiposCambioService
{
    private readonly string _connectionString;
    private readonly ILogger<TiposCambioService> _logger;

    public TiposCambioService(string connectionString, ILogger<TiposCambioService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    /// <summary>
    /// Lista todos los tipos de cambio, opcionalmente filtrados por moneda.
    /// Ordenados del mas reciente al mas antiguo.
    /// </summary>
    public async Task<List<TipoCambioItem>> GetAllAsync(int? idMoneda = null)
    {
        const string sql = @"
            SELECT TOP 50
                IdTipoCambio, TipoCambioCotizacion, TipoCambioVenta,
                IdMoneda, Moneda, IdUsuario, Nombre, FechaCaptura
            FROM vTiposCambio
            WHERE (@IdMoneda IS NULL OR IdMoneda = @IdMoneda)
            ORDER BY FechaCaptura DESC";

        using var db = CreateConnection();
        var result = await db.QueryAsync<TipoCambioItem>(sql, new { IdMoneda = idMoneda });
        return result.ToList();
    }

    /// <summary>
    /// Obtiene un tipo de cambio por su ID.
    /// </summary>
    public async Task<TipoCambioItem?> GetByIdAsync(int id)
    {
        const string sql = @"
            SELECT TOP 1
                IdTipoCambio, TipoCambioCotizacion, TipoCambioVenta,
                IdMoneda, Moneda, IdUsuario, Nombre, FechaCaptura
            FROM vTiposCambio
            WHERE IdTipoCambio = @Id";

        using var db = CreateConnection();
        return await db.QueryFirstOrDefaultAsync<TipoCambioItem>(sql, new { Id = id });
    }

    /// <summary>
    /// Obtiene el tipo de cambio mas reciente por cada moneda.
    /// </summary>
    public async Task<List<TipoCambioVigente>> GetVigentesAsync()
    {
        const string sql = @"
            SELECT TOP 50
                tc.idMoneda AS IdMoneda,
                m.Moneda,
                tc.TipoCambioCotizacion,
                tc.TipoCambioVenta,
                tc.FechaCaptura
            FROM TiposCambio tc
            INNER JOIN Monedas m ON m.IdMoneda = tc.idMoneda
            INNER JOIN (
                SELECT idMoneda, MAX(IdTipoCambio) AS MaxId
                FROM TiposCambio
                GROUP BY idMoneda
            ) ult ON ult.idMoneda = tc.idMoneda AND ult.MaxId = tc.IdTipoCambio
            ORDER BY m.Moneda";

        using var db = CreateConnection();
        var result = await db.QueryAsync<TipoCambioVigente>(sql);
        return result.ToList();
    }

    /// <summary>
    /// Catalogo de monedas para el dropdown.
    /// </summary>
    public async Task<List<MonedaItem>> GetMonedasAsync()
    {
        const string sql = @"
            SELECT TOP 50 IdMoneda, Moneda, Extranjera
            FROM Monedas
            ORDER BY Moneda";

        using var db = CreateConnection();
        var result = await db.QueryAsync<MonedaItem>(sql);
        return result.ToList();
    }

    /// <summary>
    /// Registra un nuevo tipo de cambio.
    /// Se inserta tanto en la BD local como en la conexion internet (legacy dual-write).
    /// </summary>
    public async Task<int> CreateAsync(int idMoneda, decimal tipoCambioCotizacion,
        decimal? tipoCambioVenta, int idUsuario)
    {
        const string sql = @"
            INSERT INTO TiposCambio (idMoneda, TipoCambioCotizacion, TipoCambioVenta, FechaCaptura, IdUsuario)
            VALUES (@IdMoneda, @TipoCambioCotizacion, @TipoCambioVenta, GETUTCDATE(), @IdUsuario);
            SELECT CAST(SCOPE_IDENTITY() AS INT)";

        using var db = CreateConnection();
        var id = await db.QuerySingleAsync<int>(sql, new
        {
            IdMoneda = idMoneda,
            TipoCambioCotizacion = tipoCambioCotizacion,
            TipoCambioVenta = tipoCambioVenta,
            IdUsuario = idUsuario
        });

        _logger.LogInformation(
            "TipoCambio creado: Id={Id}, Moneda={Moneda}, Cotizacion={Cotizacion}, Venta={Venta}",
            id, idMoneda, tipoCambioCotizacion, tipoCambioVenta);

        return id;
    }

    /// <summary>
    /// Actualiza un tipo de cambio existente.
    /// </summary>
    public async Task<bool> UpdateAsync(int idTipoCambio, decimal tipoCambioCotizacion,
        decimal? tipoCambioVenta)
    {
        const string sql = @"
            UPDATE TiposCambio
            SET TipoCambioCotizacion = @TipoCambioCotizacion,
                TipoCambioVenta = @TipoCambioVenta
            WHERE IdTipoCambio = @IdTipoCambio";

        using var db = CreateConnection();
        var rows = await db.ExecuteAsync(sql, new
        {
            IdTipoCambio = idTipoCambio,
            TipoCambioCotizacion = tipoCambioCotizacion,
            TipoCambioVenta = tipoCambioVenta
        });

        _logger.LogInformation("TipoCambio actualizado: Id={Id}, Rows={Rows}", idTipoCambio, rows);
        return rows > 0;
    }

    /// <summary>
    /// Elimina un tipo de cambio.
    /// </summary>
    public async Task<bool> DeleteAsync(int idTipoCambio)
    {
        const string sql = "DELETE FROM TiposCambio WHERE IdTipoCambio = @IdTipoCambio";

        using var db = CreateConnection();
        var rows = await db.ExecuteAsync(sql, new { IdTipoCambio = idTipoCambio });

        _logger.LogInformation("TipoCambio eliminado: Id={Id}, Rows={Rows}", idTipoCambio, rows);
        return rows > 0;
    }
}
