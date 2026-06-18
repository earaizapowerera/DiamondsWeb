using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio para el cálculo de Equilibrio de Comisiones.
/// Replica la lógica de frmEquilibrio.frm (VB6 RecursosHumanos.vbp):
///   Balance = Ventas comisionables - Comisiones pagadas (tabla mr).
/// Umbrales VB6: idgrupo=12 > $6,000  |  proveedor=90 > $12,000.
/// </summary>
public class EquilibrioService
{
    private readonly string _connectionString;
    private readonly ILogger<EquilibrioService> _logger;

    public EquilibrioService(string connectionString, ILogger<EquilibrioService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    /// <summary>
    /// Calcula el equilibrio de comisiones para una tienda y mes.
    /// </summary>
    /// <param name="idTienda">ID de la tienda.</param>
    /// <param name="fechaDesde">Fecha inicio del período (típicamente 1er día del mes).</param>
    /// <param name="fechaHasta">Fecha fin del período (último día del mes, opcional).</param>
    public async Task<EquilibrioResultado> CalcularEquilibrioAsync(int idTienda, DateTime fechaDesde, DateTime? fechaHasta = null)
    {
        var resultado = new EquilibrioResultado();
        using var conn = CreateConnection();

        // ── Query 1: Ventas comisionables ──
        // Origen VB6: JOIN piezasnotas + bajasnotas + vBAJASPIEZAS
        // Filtros: (monto > 6000 AND idgrupo=12) OR (monto > 12000 AND proveedor=90)
        var sqlVentas = @"
            SELECT ISNULL(SUM(pn.Total * (bn.Neto / NULLIF(bn.Bruto, 0))), 0)
              FROM piezasnotas pn
             INNER JOIN bajasnotas bn ON bn.IdNota = pn.IdNota
             INNER JOIN vBAJASPIEZAS bp ON bp.CodigoBarras = pn.CodigoBarras
             WHERE (
                       (pn.Total * (bn.Neto / NULLIF(bn.Bruto, 0)) > 6000  AND bp.IdGrupo = 12)
                    OR (pn.Total * (bn.Neto / NULLIF(bn.Bruto, 0)) > 12000 AND bp.Proveedor = 90)
                   )
               AND bn.FechaBaja >= @FechaDesde
               AND (@FechaHasta IS NULL OR bn.FechaBaja < @FechaHastaPlusOne)
               AND bn.IdTienda = @IdTienda
               AND bn.Bruto > 0";

        resultado.VentasComisionables = await conn.ExecuteScalarAsync<decimal>(sqlVentas, new
        {
            IdTienda = idTienda,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            FechaHastaPlusOne = fechaHasta?.AddDays(1)
        });

        // ── Query 2: Comisiones pagadas (tabla mr) ──
        // Origen VB6: SELECT SUM(Importe) FROM mr WHERE fecha >= '...'
        var sqlComisiones = @"
            SELECT ISNULL(SUM(Importe), 0)
              FROM mr
             WHERE Fecha >= @FechaDesde
               AND (@FechaHasta IS NULL OR Fecha < @FechaHastaPlusOne)";

        resultado.ComisionesPagadas = await conn.ExecuteScalarAsync<decimal>(sqlComisiones, new
        {
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            FechaHastaPlusOne = fechaHasta?.AddDays(1)
        });

        _logger.LogInformation(
            "Equilibrio calculado: Tienda={IdTienda}, Desde={FechaDesde:yyyy-MM-dd}, " +
            "Ventas={Ventas:C}, Comisiones={Comisiones:C}, Balance={Balance:C}",
            idTienda, fechaDesde,
            resultado.VentasComisionables, resultado.ComisionesPagadas, resultado.Balance);

        return resultado;
    }

    /// <summary>
    /// Obtiene la lista de tiendas activas para el dropdown.
    /// </summary>
    public async Task<List<TiendaItem>> ObtenerTiendasAsync()
    {
        using var conn = CreateConnection();
        var sql = "SELECT TOP 50 IdTienda, NombreTienda FROM Tiendas WHERE IdTienda > 0 ORDER BY NombreTienda";
        return (await conn.QueryAsync<TiendaItem>(sql)).ToList();
    }
}
