using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DiamondsWeb.Services;

public class CorteService
{
    private readonly string _connectionString;
    private readonly ILogger<CorteService> _logger;

    public CorteService(string connectionString, ILogger<CorteService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    /// <summary>
    /// Obtiene la fecha del último corte de caja (tabla corte, 1 sola fila).
    /// </summary>
    public async Task<DateTime?> ObtenerFechaUltimoCorteAsync()
    {
        const string sql = "SELECT TOP 1 FechaCorte FROM corte";
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<DateTime?>(sql);
    }

    /// <summary>
    /// Obtiene el resumen de ventas (BAJASNOTAS) entre dos fechas.
    /// Si fechaDesde es null, trae desde el inicio de los tiempos.
    /// </summary>
    public async Task<ResumenVentasPeriodo> ObtenerResumenVentasAsync(DateTime? fechaDesde, DateTime? fechaHasta)
    {
        const string sql = @"
            SELECT
                COUNT(*) AS TotalNotas,
                ISNULL(SUM(Bruto), 0) AS TotalBruto,
                ISNULL(SUM(Bruto - Neto), 0) AS TotalDescuento,
                ISNULL(SUM(Neto), 0) AS TotalNeto,
                ISNULL(SUM(Impuesto), 0) AS TotalImpuesto,
                ISNULL(SUM(Total), 0) AS TotalVenta
            FROM BAJASNOTAS
            WHERE (@FechaDesde IS NULL OR FechaBaja >= @FechaDesde)
              AND (@FechaHasta IS NULL OR FechaBaja <= @FechaHasta)";

        using var conn = CreateConnection();
        return await conn.QueryFirstAsync<ResumenVentasPeriodo>(sql, new
        {
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta
        });
    }

    /// <summary>
    /// Obtiene el desglose de ventas por forma de pago entre dos fechas.
    /// </summary>
    public async Task<List<VentaPorFormaPago>> ObtenerVentasPorFormaPagoAsync(DateTime? fechaDesde, DateTime? fechaHasta)
    {
        const string sql = @"
            SELECT
                ISNULL(FormaPago, 'Sin especificar') AS FormaPago,
                COUNT(*) AS CantidadNotas,
                ISNULL(SUM(Total), 0) AS Total
            FROM BAJASNOTAS
            WHERE (@FechaDesde IS NULL OR FechaBaja >= @FechaDesde)
              AND (@FechaHasta IS NULL OR FechaBaja <= @FechaHasta)
            GROUP BY FormaPago
            ORDER BY Total DESC";

        using var conn = CreateConnection();
        return (await conn.QueryAsync<VentaPorFormaPago>(sql, new
        {
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta
        })).ToList();
    }

    /// <summary>
    /// Ejecuta el corte de caja: actualiza tabla corte (DELETE+INSERT) y registra en historial.
    /// Replica el comportamiento VB6: la tabla corte siempre tiene 1 fila.
    /// </summary>
    public async Task<CorteHistorial> RealizarCorteAsync(int idUsuario, string? comentario)
    {
        using var conn = CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        try
        {
            // 1. Obtener fecha del corte anterior
            var fechaAnterior = await conn.QueryFirstOrDefaultAsync<DateTime?>(
                "SELECT TOP 1 FechaCorte FROM corte", transaction: tx);

            // 2. Obtener resumen de ventas del período que se cierra
            var resumen = await conn.QueryFirstAsync<ResumenVentasPeriodo>(@"
                SELECT
                    COUNT(*) AS TotalNotas,
                    ISNULL(SUM(Bruto), 0) AS TotalBruto,
                    ISNULL(SUM(Bruto - Neto), 0) AS TotalDescuento,
                    ISNULL(SUM(Neto), 0) AS TotalNeto,
                    ISNULL(SUM(Impuesto), 0) AS TotalImpuesto,
                    ISNULL(SUM(Total), 0) AS TotalVenta
                FROM BAJASNOTAS
                WHERE (@FechaDesde IS NULL OR FechaBaja >= @FechaDesde)",
                new { FechaDesde = fechaAnterior }, transaction: tx);

            // 3. Ejecutar corte (comportamiento VB6: DELETE + INSERT)
            await conn.ExecuteAsync(
                "DELETE corte; INSERT INTO corte (FechaCorte) VALUES (GETUTCDATE())",
                transaction: tx);

            // 4. Obtener la fecha recién insertada
            var fechaNueva = await conn.QueryFirstAsync<DateTime>(
                "SELECT TOP 1 FechaCorte FROM corte", transaction: tx);

            // 5. Registrar en historial
            var historialId = await conn.QuerySingleAsync<int>(@"
                INSERT INTO cortes_historial (FechaCorte, FechaCorteAnterior, IdUsuario, TotalNotas, TotalVentas, Comentario, FechaRegistro)
                VALUES (@FechaCorte, @FechaCorteAnterior, @IdUsuario, @TotalNotas, @TotalVentas, @Comentario, GETUTCDATE());
                SELECT SCOPE_IDENTITY()",
                new
                {
                    FechaCorte = fechaNueva,
                    FechaCorteAnterior = fechaAnterior,
                    IdUsuario = idUsuario,
                    TotalNotas = resumen.TotalNotas,
                    TotalVentas = resumen.TotalVenta,
                    Comentario = comentario
                }, transaction: tx);

            tx.Commit();

            _logger.LogInformation("Corte de caja realizado por usuario {UserId}. Fecha anterior: {Anterior}, Nueva: {Nueva}, Notas: {Notas}, Total: {Total}",
                idUsuario, fechaAnterior, fechaNueva, resumen.TotalNotas, resumen.TotalVenta);

            return new CorteHistorial
            {
                Id = historialId,
                FechaCorte = fechaNueva,
                FechaCorteAnterior = fechaAnterior,
                IdUsuario = idUsuario,
                TotalNotas = resumen.TotalNotas,
                TotalVentas = resumen.TotalVenta,
                Comentario = comentario,
                FechaRegistro = fechaNueva
            };
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Obtiene el historial de cortes realizados, con nombre del usuario.
    /// </summary>
    public async Task<List<CorteHistorial>> ObtenerHistorialAsync(int top = 50)
    {
        var sql = $@"
            SELECT TOP {top}
                ch.Id,
                ch.FechaCorte,
                ch.FechaCorteAnterior,
                ch.IdUsuario,
                ISNULL(u.Nombre, 'Desconocido') AS NombreUsuario,
                ch.TotalNotas,
                ch.TotalVentas,
                ch.Comentario,
                ch.FechaRegistro
            FROM cortes_historial ch
            LEFT JOIN Usuarios u ON u.IdUsuario = ch.IdUsuario
            ORDER BY ch.FechaCorte DESC";

        using var conn = CreateConnection();
        return (await conn.QueryAsync<CorteHistorial>(sql)).ToList();
    }
}
