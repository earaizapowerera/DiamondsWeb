using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio para consultar cuentas de consignación.
/// Tres estados: En Existencia (PIEZAS), Por Devolver (BAJASPIEZAS vendidas), Devuelto (BAJASREMISIONES).
/// </summary>
public class ConsignacionService
{
    private readonly string _connectionString;
    private readonly ILogger<ConsignacionService> _logger;

    public ConsignacionService(string connectionString, ILogger<ConsignacionService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    /// <summary>
    /// Piezas en existencia: están en PIEZAS y su remisión es de consignación.
    /// Filtro opcional por IdRemision y fecha desde.
    /// </summary>
    public async Task<List<PiezaConsignacion>> ObtenerEnExistenciaAsync(int? idRemision, DateTime? fechaDesde)
    {
        var sql = @"
            SELECT TOP 50
                p.CodigoBarras, p.Descripcion, p.IdRemision,
                r.Remision, pr.NombreProveedor,
                r.FechaRemision, p.Peso, p.CBTotal,
                p.Kilates, p.Modelo,
                ISNULL(s.NombreStatus, 'N/A') AS NombreStatus
            FROM PIEZAS p
            INNER JOIN REMISIONES r ON r.IdRemision = p.IdRemision
            LEFT JOIN PROVEEDORES pr ON pr.Proveedor = r.Proveedor
            LEFT JOIN StatusPiezas s ON s.IdStatus = p.IdStatus
            WHERE r.Consignacion = 1
              AND (@IdRemision IS NULL OR p.IdRemision = @IdRemision)
              AND (@FechaDesde IS NULL OR r.FechaRemision >= @FechaDesde)
            ORDER BY r.FechaRemision DESC, p.CodigoBarras";

        try
        {
            using var conn = CreateConnection();
            return (await conn.QueryAsync<PiezaConsignacion>(sql, new
            {
                IdRemision = idRemision,
                FechaDesde = fechaDesde
            })).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar piezas en existencia consignación");
            throw;
        }
    }

    /// <summary>
    /// Piezas vendidas (baja): están en BAJASPIEZAS y su remisión es de consignación.
    /// Representan piezas que se vendieron y se debe liquidar con el proveedor.
    /// </summary>
    public async Task<List<PiezaConsignacion>> ObtenerPorDevolverAsync(int? idRemision, DateTime? fechaDesde)
    {
        var sql = @"
            SELECT TOP 50
                bp.CodigoBarras, bp.Descripcion, bp.IdRemision,
                r.Remision, pr.NombreProveedor,
                r.FechaRemision, bp.Peso, bp.CBTotal,
                bp.Kilates, bp.Modelo,
                ISNULL(s.NombreStatus, 'N/A') AS NombreStatus
            FROM BAJASPIEZAS bp
            INNER JOIN REMISIONES r ON r.IdRemision = bp.IdRemision
            LEFT JOIN PROVEEDORES pr ON pr.Proveedor = r.Proveedor
            LEFT JOIN StatusPiezas s ON s.IdStatus = bp.IdStatus
            WHERE r.Consignacion = 1
              AND NOT EXISTS (SELECT 1 FROM BAJASREMISIONES br WHERE br.IdRemision = bp.IdRemision)
              AND (@IdRemision IS NULL OR bp.IdRemision = @IdRemision)
              AND (@FechaDesde IS NULL OR r.FechaRemision >= @FechaDesde)
            ORDER BY r.FechaRemision DESC, bp.CodigoBarras";

        try
        {
            using var conn = CreateConnection();
            return (await conn.QueryAsync<PiezaConsignacion>(sql, new
            {
                IdRemision = idRemision,
                FechaDesde = fechaDesde
            })).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar piezas por devolver consignación");
            throw;
        }
    }

    /// <summary>
    /// Piezas devueltas: están en BAJASPIEZAS y su remisión está en BAJASREMISIONES (liquidada/devuelta).
    /// </summary>
    public async Task<List<PiezaConsignacion>> ObtenerDevueltasAsync(int? idRemision, DateTime? fechaDesde)
    {
        var sql = @"
            SELECT TOP 50
                bp.CodigoBarras, bp.Descripcion, bp.IdRemision,
                r.Remision, pr.NombreProveedor,
                r.FechaRemision, bp.Peso, bp.CBTotal,
                bp.Kilates, bp.Modelo,
                ISNULL(s.NombreStatus, 'N/A') AS NombreStatus
            FROM BAJASPIEZAS bp
            INNER JOIN REMISIONES r ON r.IdRemision = bp.IdRemision
            INNER JOIN BAJASREMISIONES br ON br.IdRemision = bp.IdRemision
            LEFT JOIN PROVEEDORES pr ON pr.Proveedor = r.Proveedor
            LEFT JOIN StatusPiezas s ON s.IdStatus = bp.IdStatus
            WHERE r.Consignacion = 1
              AND (@IdRemision IS NULL OR bp.IdRemision = @IdRemision)
              AND (@FechaDesde IS NULL OR r.FechaRemision >= @FechaDesde)
            ORDER BY r.FechaRemision DESC, bp.CodigoBarras";

        try
        {
            using var conn = CreateConnection();
            return (await conn.QueryAsync<PiezaConsignacion>(sql, new
            {
                IdRemision = idRemision,
                FechaDesde = fechaDesde
            })).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar piezas devueltas consignación");
            throw;
        }
    }

    /// <summary>
    /// Estadísticas generales de consignación
    /// </summary>
    public async Task<ConsignacionStats> ObtenerEstadisticasAsync(int? idRemision, DateTime? fechaDesde)
    {
        var sql = @"
            SELECT TOP 1
                (SELECT COUNT(*) FROM PIEZAS p
                 INNER JOIN REMISIONES r ON r.IdRemision = p.IdRemision
                 WHERE r.Consignacion = 1
                   AND (@IdRemision IS NULL OR p.IdRemision = @IdRemision)
                   AND (@FechaDesde IS NULL OR r.FechaRemision >= @FechaDesde)
                ) AS PiezasEnExistencia,
                (SELECT ISNULL(SUM(p.CBTotal), 0) FROM PIEZAS p
                 INNER JOIN REMISIONES r ON r.IdRemision = p.IdRemision
                 WHERE r.Consignacion = 1
                   AND (@IdRemision IS NULL OR p.IdRemision = @IdRemision)
                   AND (@FechaDesde IS NULL OR r.FechaRemision >= @FechaDesde)
                ) AS MontoEnExistencia,
                (SELECT COUNT(*) FROM BAJASPIEZAS bp
                 INNER JOIN REMISIONES r ON r.IdRemision = bp.IdRemision
                 WHERE r.Consignacion = 1
                   AND NOT EXISTS (SELECT 1 FROM BAJASREMISIONES br WHERE br.IdRemision = bp.IdRemision)
                   AND (@IdRemision IS NULL OR bp.IdRemision = @IdRemision)
                   AND (@FechaDesde IS NULL OR r.FechaRemision >= @FechaDesde)
                ) AS PiezasPorDevolver,
                (SELECT ISNULL(SUM(bp.CBTotal), 0) FROM BAJASPIEZAS bp
                 INNER JOIN REMISIONES r ON r.IdRemision = bp.IdRemision
                 WHERE r.Consignacion = 1
                   AND NOT EXISTS (SELECT 1 FROM BAJASREMISIONES br WHERE br.IdRemision = bp.IdRemision)
                   AND (@IdRemision IS NULL OR bp.IdRemision = @IdRemision)
                   AND (@FechaDesde IS NULL OR r.FechaRemision >= @FechaDesde)
                ) AS MontoPorDevolver,
                (SELECT COUNT(*) FROM BAJASPIEZAS bp
                 INNER JOIN REMISIONES r ON r.IdRemision = bp.IdRemision
                 INNER JOIN BAJASREMISIONES br ON br.IdRemision = bp.IdRemision
                 WHERE r.Consignacion = 1
                   AND (@IdRemision IS NULL OR bp.IdRemision = @IdRemision)
                   AND (@FechaDesde IS NULL OR r.FechaRemision >= @FechaDesde)
                ) AS PiezasDevueltas,
                (SELECT ISNULL(SUM(bp.CBTotal), 0) FROM BAJASPIEZAS bp
                 INNER JOIN REMISIONES r ON r.IdRemision = bp.IdRemision
                 INNER JOIN BAJASREMISIONES br ON br.IdRemision = bp.IdRemision
                 WHERE r.Consignacion = 1
                   AND (@IdRemision IS NULL OR bp.IdRemision = @IdRemision)
                   AND (@FechaDesde IS NULL OR r.FechaRemision >= @FechaDesde)
                ) AS MontoDevueltas,
                (SELECT COUNT(DISTINCT r.IdRemision) FROM REMISIONES r
                 WHERE r.Consignacion = 1
                   AND (@IdRemision IS NULL OR r.IdRemision = @IdRemision)
                   AND (@FechaDesde IS NULL OR r.FechaRemision >= @FechaDesde)
                ) AS TotalRemisiones";

        try
        {
            using var conn = CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<ConsignacionStats>(sql, new
            {
                IdRemision = idRemision,
                FechaDesde = fechaDesde
            }) ?? new ConsignacionStats();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener estadísticas de consignación");
            throw;
        }
    }

    /// <summary>
    /// Lista de remisiones de consignación para el dropdown de filtro
    /// </summary>
    public async Task<List<RemisionConsignacionResumen>> ObtenerRemisionesAsync()
    {
        var sql = @"
            SELECT TOP 50
                r.IdRemision, r.Remision, pr.NombreProveedor,
                r.FechaRemision,
                COUNT(p.CodigoBarras) AS TotalPiezas,
                ISNULL(SUM(p.CBTotal), 0) AS MontoTotal
            FROM REMISIONES r
            LEFT JOIN PROVEEDORES pr ON pr.Proveedor = r.Proveedor
            LEFT JOIN PIEZAS p ON p.IdRemision = r.IdRemision
            WHERE r.Consignacion = 1
            GROUP BY r.IdRemision, r.Remision, pr.NombreProveedor, r.FechaRemision
            ORDER BY r.FechaRemision DESC";

        try
        {
            using var conn = CreateConnection();
            return (await conn.QueryAsync<RemisionConsignacionResumen>(sql)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener listado de remisiones de consignación");
            throw;
        }
    }
}
