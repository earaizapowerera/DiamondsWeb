using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio para gestionar remisiones de proveedor y vinculacion de piezas.
/// Migrado de frmActualizacionRemisiones.frm (VB6 legacy).
/// Tablas: REMISIONES, PIEZAS, PROVEEDORES.
/// Vistas: vBuscaRemisiones, vActualizaPiezas.
/// </summary>
public class RemisionService
{
    private readonly string _connectionString;
    private readonly ILogger<RemisionService> _logger;

    public RemisionService(string connectionString, ILogger<RemisionService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    /// <summary>
    /// Buscar remisiones con filtros opcionales.
    /// </summary>
    public async Task<List<RemisionResumen>> BuscarRemisionesAsync(
        string? buscar, int? proveedorId, bool? soloConsignacion)
    {
        using var conn = CreateConnection();

        var sql = @"
            SELECT TOP 50
                r.IdRemision, r.Proveedor, r.NombreProveedor,
                r.Remision, r.FechaRemision, r.Consignacion,
                r.IdTienda, r.FechaCaptura, r.FechaUltEdicion,
                ISNULL(p.TotalPiezas, 0) AS TotalPiezas,
                ISNULL(p.TotalBruto, 0) AS TotalBruto,
                ISNULL(p.TotalNeto, 0) AS TotalNeto
            FROM vBuscaRemisiones r
            LEFT JOIN (
                SELECT IdRemision,
                       COUNT(*) AS TotalPiezas,
                       ISNULL(SUM(CBTotal * ISNULL(TCCosto, 1)), 0) AS TotalBruto,
                       ISNULL(SUM(CNTotal * ISNULL(TCCosto, 1)), 0) AS TotalNeto
                FROM PIEZAS
                WHERE IdRemision IS NOT NULL
                GROUP BY IdRemision
            ) p ON p.IdRemision = r.IdRemision
            WHERE 1=1
                AND (@Buscar IS NULL
                     OR r.NombreProveedor LIKE '%' + @Buscar + '%'
                     OR r.Remision LIKE '%' + @Buscar + '%'
                     OR CAST(r.IdRemision AS VARCHAR) LIKE '%' + @Buscar + '%')
                AND (@ProveedorId IS NULL OR r.Proveedor = @ProveedorId)
                AND (@SoloConsignacion IS NULL OR r.Consignacion = @SoloConsignacion)
            ORDER BY r.IdRemision DESC";

        var result = await conn.QueryAsync<RemisionResumen>(sql, new
        {
            Buscar = buscar,
            ProveedorId = proveedorId,
            SoloConsignacion = soloConsignacion
        });
        return result.ToList();
    }

    /// <summary>
    /// Obtener una remision por su Id.
    /// </summary>
    public async Task<RemisionResumen?> ObtenerRemisionAsync(int idRemision)
    {
        using var conn = CreateConnection();

        var sql = @"
            SELECT TOP 1
                r.IdRemision, r.Proveedor, r.NombreProveedor,
                r.Remision, r.FechaRemision, r.Consignacion,
                r.IdTienda, r.FechaCaptura, r.FechaUltEdicion,
                ISNULL(p.TotalPiezas, 0) AS TotalPiezas,
                ISNULL(p.TotalBruto, 0) AS TotalBruto,
                ISNULL(p.TotalNeto, 0) AS TotalNeto
            FROM vBuscaRemisiones r
            LEFT JOIN (
                SELECT IdRemision,
                       COUNT(*) AS TotalPiezas,
                       ISNULL(SUM(CBTotal * ISNULL(TCCosto, 1)), 0) AS TotalBruto,
                       ISNULL(SUM(CNTotal * ISNULL(TCCosto, 1)), 0) AS TotalNeto
                FROM PIEZAS
                WHERE IdRemision IS NOT NULL
                GROUP BY IdRemision
            ) p ON p.IdRemision = r.IdRemision
            WHERE r.IdRemision = @IdRemision";

        return await conn.QueryFirstOrDefaultAsync<RemisionResumen>(sql, new { IdRemision = idRemision });
    }

    /// <summary>
    /// Crear nueva remision. Genera IdRemision desde tabla contador.
    /// </summary>
    public async Task<int> CrearRemisionAsync(
        int proveedor, string remision, DateTime? fechaRemision,
        bool consignacion, int idUsuario)
    {
        using var conn = CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        try
        {
            // Obtener siguiente Id de remision desde contador (mismo patron que VB6)
            var nextId = await conn.QueryFirstAsync<int>(
                "SELECT ISNULL(Remision, 0) + 1 FROM contador",
                transaction: tx);

            await conn.ExecuteAsync(
                "UPDATE contador SET Remision = @NextId",
                new { NextId = nextId },
                transaction: tx);

            // Insertar remision
            await conn.ExecuteAsync(@"
                INSERT INTO REMISIONES (IdRemision, Proveedor, Remision, FechaRemision,
                    Consignacion, IdUsuario, FechaCaptura, FechaUltEdicion)
                VALUES (@IdRemision, @Proveedor, @Remision, @FechaRemision,
                    @Consignacion, @IdUsuario, GETUTCDATE(), GETUTCDATE())",
                new
                {
                    IdRemision = nextId,
                    Proveedor = proveedor,
                    Remision = remision,
                    FechaRemision = fechaRemision,
                    Consignacion = consignacion,
                    IdUsuario = idUsuario
                },
                transaction: tx);

            tx.Commit();
            _logger.LogInformation("Remision creada: IdRemision={Id}, Proveedor={Prov}", nextId, proveedor);
            return nextId;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Actualizar datos de una remision existente.
    /// </summary>
    public async Task ActualizarRemisionAsync(
        int idRemision, int proveedor, string remision,
        DateTime? fechaRemision, bool consignacion)
    {
        using var conn = CreateConnection();

        await conn.ExecuteAsync(@"
            UPDATE REMISIONES
            SET Proveedor = @Proveedor,
                Remision = @Remision,
                FechaRemision = @FechaRemision,
                Consignacion = @Consignacion,
                FechaUltEdicion = GETUTCDATE()
            WHERE IdRemision = @IdRemision",
            new
            {
                IdRemision = idRemision,
                Proveedor = proveedor,
                Remision = remision,
                FechaRemision = fechaRemision,
                Consignacion = consignacion
            });

        _logger.LogInformation("Remision actualizada: IdRemision={Id}", idRemision);
    }

    /// <summary>
    /// Eliminar remision si no tiene piezas vinculadas.
    /// </summary>
    public async Task<bool> EliminarRemisionAsync(int idRemision)
    {
        using var conn = CreateConnection();

        // Verificar que no tenga piezas vinculadas
        var piezasCount = await conn.ExecuteScalarAsync<int>(
            "SELECT TOP 1 COUNT(*) FROM PIEZAS WHERE IdRemision = @Id",
            new { Id = idRemision });

        if (piezasCount > 0)
        {
            _logger.LogWarning("No se puede eliminar remision {Id}: tiene {Count} piezas vinculadas",
                idRemision, piezasCount);
            return false;
        }

        await conn.ExecuteAsync(
            "DELETE FROM REMISIONES WHERE IdRemision = @Id",
            new { Id = idRemision });

        _logger.LogInformation("Remision eliminada: IdRemision={Id}", idRemision);
        return true;
    }

    /// <summary>
    /// Obtener piezas disponibles (sin remision o con otra remision) filtradas por busqueda.
    /// </summary>
    public async Task<List<PiezaDisponible>> ObtenerPiezasDisponiblesAsync(
        int idRemision, string? buscar)
    {
        using var conn = CreateConnection();

        var sql = @"
            SELECT TOP 50
                CodigoBarras, Obs2, IdFactura, IdRemision,
                Remision, Proveedor, Descripcion, FechaCaptura,
                TCCosto, CBPieza, CNPieza, DescPieza,
                CostoMN, IdMoneda, CostoBrutoMN
            FROM vActualizaPiezas
            WHERE (IdRemision IS NULL OR IdRemision <> @IdRemision)
                AND (@Buscar IS NULL
                     OR CodigoBarras LIKE '%' + @Buscar + '%'
                     OR Obs2 LIKE '%' + @Buscar + '%'
                     OR Descripcion LIKE '%' + @Buscar + '%')
            ORDER BY FechaCaptura DESC";

        var result = await conn.QueryAsync<PiezaDisponible>(sql, new
        {
            IdRemision = idRemision,
            Buscar = buscar
        });
        return result.ToList();
    }

    /// <summary>
    /// Obtener piezas vinculadas a una remision.
    /// </summary>
    public async Task<List<PiezaRemision>> ObtenerPiezasRemisionAsync(int idRemision)
    {
        using var conn = CreateConnection();

        var sql = @"
            SELECT TOP 50
                CodigoBarras,
                Obs2,
                CBTotal,
                CNTotal,
                TCCosto,
                CBTotal * ISNULL(TCCosto, 1) AS Bruto,
                CNTotal * ISNULL(TCCosto, 1) AS Neto
            FROM PIEZAS
            WHERE IdRemision = @IdRemision
            ORDER BY CodigoBarras";

        var result = await conn.QueryAsync<PiezaRemision>(sql, new { IdRemision = idRemision });
        return result.ToList();
    }

    /// <summary>
    /// Obtener totales bruto/neto de una remision.
    /// </summary>
    public async Task<RemisionTotales> ObtenerTotalesRemisionAsync(int idRemision)
    {
        using var conn = CreateConnection();

        var sql = @"
            SELECT TOP 1
                ISNULL(SUM(CBTotal * ISNULL(TCCosto, 1)), 0) AS Bruto,
                ISNULL(SUM(CNTotal * ISNULL(TCCosto, 1)), 0) AS Neto
            FROM PIEZAS
            WHERE IdRemision = @IdRemision";

        return await conn.QueryFirstAsync<RemisionTotales>(sql, new { IdRemision = idRemision });
    }

    /// <summary>
    /// Vincular una pieza a una remision (Enter en grid izquierdo del VB6).
    /// </summary>
    public async Task VincularPiezaAsync(int idRemision, string codigoBarras)
    {
        using var conn = CreateConnection();

        await conn.ExecuteAsync(@"
            UPDATE PIEZAS
            SET IdRemision = @IdRemision,
                FechaUltEdicion = GETUTCDATE()
            WHERE CodigoBarras = @CodigoBarras",
            new { IdRemision = idRemision, CodigoBarras = codigoBarras });

        _logger.LogInformation("Pieza {CB} vinculada a remision {Id}", codigoBarras, idRemision);
    }

    /// <summary>
    /// Desvincular una pieza de una remision (Enter en grid derecho del VB6).
    /// </summary>
    public async Task DesvincularPiezaAsync(string codigoBarras)
    {
        using var conn = CreateConnection();

        await conn.ExecuteAsync(@"
            UPDATE PIEZAS
            SET IdRemision = NULL,
                FechaUltEdicion = GETUTCDATE()
            WHERE CodigoBarras = @CodigoBarras",
            new { CodigoBarras = codigoBarras });

        _logger.LogInformation("Pieza {CB} desvinculada de remision", codigoBarras);
    }

    /// <summary>
    /// Vincular remision completa: mover todas las piezas de otra remision a esta.
    /// Boton "Actualizar Remision Completa" del VB6.
    /// </summary>
    public async Task VincularRemisionCompletaAsync(int idRemisionDestino, int idRemisionOrigen)
    {
        using var conn = CreateConnection();

        var affected = await conn.ExecuteAsync(@"
            UPDATE PIEZAS
            SET IdRemision = @IdDestino,
                FechaUltEdicion = GETUTCDATE()
            WHERE IdRemision = @IdOrigen",
            new { IdDestino = idRemisionDestino, IdOrigen = idRemisionOrigen });

        _logger.LogInformation(
            "Remision completa: {Count} piezas movidas de remision {Origen} a {Destino}",
            affected, idRemisionOrigen, idRemisionDestino);
    }

    /// <summary>
    /// Obtener lista de proveedores para dropdown.
    /// </summary>
    public async Task<List<ProveedorItem>> ObtenerProveedoresAsync()
    {
        using var conn = CreateConnection();

        var sql = @"
            SELECT TOP 500 Proveedor, NombreProveedor
            FROM PROVEEDORES
            ORDER BY NombreProveedor";

        var result = await conn.QueryAsync<ProveedorItem>(sql);
        return result.ToList();
    }

    /// <summary>
    /// Buscar proveedores por nombre (para dropdown searchable).
    /// </summary>
    public async Task<List<ProveedorItem>> BuscarProveedoresAsync(string? buscar)
    {
        using var conn = CreateConnection();

        var sql = @"
            SELECT TOP 500 Proveedor, NombreProveedor
            FROM PROVEEDORES
            WHERE @Buscar IS NULL
               OR NombreProveedor LIKE '%' + @Buscar + '%'
               OR CAST(Proveedor AS VARCHAR) LIKE '%' + @Buscar + '%'
            ORDER BY NombreProveedor";

        var result = await conn.QueryAsync<ProveedorItem>(sql, new { Buscar = buscar });
        return result.ToList();
    }

    /// <summary>
    /// Verificar conectividad a BD.
    /// </summary>
    public async Task<string> TestConexionAsync()
    {
        try
        {
            using var conn = CreateConnection();
            var count = await conn.ExecuteScalarAsync<int>(
                "SELECT TOP 1 COUNT(*) FROM REMISIONES");
            return $"OK - {count} remisiones en BD";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}
