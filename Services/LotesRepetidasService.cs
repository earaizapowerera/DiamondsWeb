using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio de Alta de Lotes de Piezas Repetidas.
/// Migración de frmLotesRepetidas.frm (VB6) a .NET 9.
/// Lógica de precio: CostoNeto × Utilidad × UtilidadExtra × Impuesto / Divisor × TCCotizacion
/// </summary>
public class LotesRepetidasService
{
    private readonly string _connectionString;
    private readonly ILogger<LotesRepetidasService> _logger;

    public LotesRepetidasService(string connectionString, ILogger<LotesRepetidasService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    // ─── Catálogos ───────────────────────────────────────────────

    /// <summary>
    /// Lista todas las monedas disponibles
    /// </summary>
    public async Task<List<Moneda>> ObtenerMonedasAsync()
    {
        using var db = CreateConnection();
        var result = await db.QueryAsync<Moneda>(
            "SELECT TOP 50 IdMoneda, Moneda AS NombreMoneda, Extranjera FROM Monedas ORDER BY IdMoneda");
        return result.ToList();
    }

    /// <summary>
    /// Obtiene el tipo de cambio más reciente para una moneda
    /// </summary>
    public async Task<TipoCambio?> ObtenerTipoCambioAsync(int idMoneda)
    {
        using var db = CreateConnection();
        return await db.QueryFirstOrDefaultAsync<TipoCambio>(
            @"SELECT TOP 1 IdTipoCambio, IdMoneda, TipoCambioCotizacion, TipoCambioVenta
              FROM tiposcambio WHERE idMoneda = @IdMoneda ORDER BY FechaCaptura DESC",
            new { IdMoneda = idMoneda });
    }

    /// <summary>
    /// Busca proveedores por nombre (para dropdown searchable)
    /// </summary>
    public async Task<List<ProveedorConDefaults>> BuscarProveedoresAsync(string? filtro = null)
    {
        using var db = CreateConnection();
        var sql = @"SELECT TOP 50 Proveedor, NombreProveedor, IdMoneda, Moneda,
                       UtilidadExtra, CaracteristicaDefault, CostoDefault,
                       DefaultUtilidadOro, DefaultUtilidadGemas, DefaultUtilidadReloj,
                       DefaultUtilidadExtra, DefaultUtilidad, UtilizarMoneda
                    FROM vProveedores";
        if (!string.IsNullOrWhiteSpace(filtro))
            sql += " WHERE NombreProveedor LIKE @Filtro";
        sql += " ORDER BY NombreProveedor";

        return (await db.QueryAsync<ProveedorConDefaults>(sql,
            new { Filtro = $"%{filtro}%" })).ToList();
    }

    /// <summary>
    /// Obtiene un proveedor con sus defaults
    /// </summary>
    public async Task<ProveedorConDefaults?> ObtenerProveedorAsync(int idProveedor)
    {
        using var db = CreateConnection();
        return await db.QueryFirstOrDefaultAsync<ProveedorConDefaults>(
            @"SELECT TOP 1 Proveedor, NombreProveedor, IdMoneda, Moneda,
                     UtilidadExtra, CaracteristicaDefault, CostoDefault,
                     DefaultUtilidadOro, DefaultUtilidadGemas, DefaultUtilidadReloj,
                     DefaultUtilidadExtra, DefaultUtilidad, UtilizarMoneda
              FROM vProveedores WHERE Proveedor = @Id",
            new { Id = idProveedor });
    }

    /// <summary>
    /// Obtiene los defaults de impuesto y divisor más recientes
    /// </summary>
    public async Task<DefaultsFactorComunes?> ObtenerDefaultsFactorAsync()
    {
        using var db = CreateConnection();
        return await db.QueryFirstOrDefaultAsync<DefaultsFactorComunes>(
            "SELECT TOP 1 DefaultImpuesto, DefaultDivisor FROM defaultsfactorcomunes ORDER BY FechaCaptura DESC");
    }

    /// <summary>
    /// Obtiene los rangos de utilidad extra por precio/gramo
    /// </summary>
    public async Task<List<UtilidadExtraPrecioGramo>> ObtenerRangosUtilidadExtraAsync()
    {
        using var db = CreateConnection();
        return (await db.QueryAsync<UtilidadExtraPrecioGramo>(
            @"SELECT TOP 50 Id, PrecioGramoDesde, PrecioGramoHasta, DefaultUtilidadExtra
              FROM utilidadextra_preciogramo ORDER BY PrecioGramoDesde")).ToList();
    }

    /// <summary>
    /// Busca una pieza en el catálogo de repetidas por código de barras
    /// </summary>
    public async Task<CatalogoRepetida?> BuscarCatalogoAsync(string codigoBarras)
    {
        using var db = CreateConnection();
        return await db.QueryFirstOrDefaultAsync<CatalogoRepetida>(
            @"SELECT TOP 1 CodigoBarras, Descripcion, Proveedor, IdGrupo, Kilates, Precio, IdDivisor
              FROM catalogorepetidas WHERE CodigoBarras = @Codigo",
            new { Codigo = codigoBarras });
    }

    /// <summary>
    /// Actualiza el precio de una pieza en el catálogo de repetidas
    /// </summary>
    public async Task ActualizarPrecioCatalogoAsync(string codigoBarras, int nuevoPrecio)
    {
        using var db = CreateConnection();
        await db.ExecuteAsync(
            "UPDATE catalogorepetidas SET Precio = @Precio WHERE CodigoBarras = @Codigo",
            new { Precio = nuevoPrecio, Codigo = codigoBarras });
    }

    // ─── Remisiones ──────────────────────────────────────────────

    /// <summary>
    /// Busca remisiones (para el selector de remisión)
    /// </summary>
    public async Task<List<Remision>> BuscarRemisionesAsync(string? filtro = null)
    {
        using var db = CreateConnection();
        var sql = @"SELECT TOP 50 r.IdRemision, r.Proveedor, r.Remision AS NumRemision,
                       r.FechaRemision, r.Consignacion, p.NombreProveedor
                    FROM Remisiones r
                    LEFT JOIN Proveedores p ON r.Proveedor = p.Proveedor";
        if (!string.IsNullOrWhiteSpace(filtro))
            sql += " WHERE p.NombreProveedor LIKE @Filtro OR CAST(r.IdRemision AS VARCHAR) LIKE @Filtro";
        sql += " ORDER BY r.IdRemision DESC";

        return (await db.QueryAsync<Remision>(sql, new { Filtro = $"%{filtro}%" })).ToList();
    }

    /// <summary>
    /// Obtiene una remisión por su ID
    /// </summary>
    public async Task<Remision?> ObtenerRemisionAsync(int idRemision)
    {
        using var db = CreateConnection();
        return await db.QueryFirstOrDefaultAsync<Remision>(
            @"SELECT TOP 1 r.IdRemision, r.Proveedor, r.Remision AS NumRemision,
                     r.FechaRemision, r.Consignacion, p.NombreProveedor
              FROM Remisiones r
              LEFT JOIN Proveedores p ON r.Proveedor = p.Proveedor
              WHERE r.IdRemision = @Id",
            new { Id = idRemision });
    }

    /// <summary>
    /// Crea una nueva remisión. Genera IdRemision con el contador.
    /// </summary>
    public async Task<int> CrearRemisionAsync(int proveedor, string? numRemision,
        DateTime fechaRemision, bool consignacion, int idTienda)
    {
        using var db = CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();

        try
        {
            // Obtener y actualizar contador
            var contadorActual = await db.ExecuteScalarAsync<int>(
                "SELECT ISNULL(remision, 0) FROM contador", transaction: tx);
            await db.ExecuteAsync(
                "UPDATE contador SET remision = ISNULL(remision, 0) + 1", transaction: tx);

            var idRemision = (idTienda * 10000) + contadorActual + 1;

            await db.ExecuteAsync(
                @"INSERT INTO Remisiones (IdRemision, Proveedor, Remision, FechaRemision,
                    Consignacion, IdUsuario, FechaCaptura, FechaUltEdicion, IdTienda, IdLocalizacion)
                  VALUES (@IdRemision, @Proveedor, @Remision, @FechaRemision,
                    @Consignacion, 1, GETUTCDATE(), GETUTCDATE(), @IdTienda, @IdTienda)",
                new
                {
                    IdRemision = idRemision,
                    Proveedor = proveedor,
                    Remision = numRemision,
                    FechaRemision = fechaRemision,
                    Consignacion = consignacion,
                    IdTienda = idTienda
                }, transaction: tx);

            tx.Commit();
            _logger.LogInformation("Remisión creada: IdRemision={Id}, Proveedor={Prov}", idRemision, proveedor);
            return idRemision;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    // ─── Piezas en Lote ──────────────────────────────────────────

    /// <summary>
    /// Obtiene las piezas de un lote por IdRemision
    /// </summary>
    public async Task<List<LoteRepetidaItem>> ObtenerPiezasPorRemisionAsync(int idRemision)
    {
        using var db = CreateConnection();
        return (await db.QueryAsync<LoteRepetidaItem>(
            @"SELECT TOP 50 Descripcion, IdLote, CodigoBarras, IdRemision, IdFactura,
                     Cantidad, Peso, PrecioGramo, CostoBruto, Descuento, CostoNeto,
                     IdMoneda, FechaCaptura, FechaUltEdicion, IdUsuario,
                     IdTienda, IdLocalizacion, TCCosto, TCCotizacion, Precio, Nombre
              FROM vLotesRepetidas
              WHERE IdRemision = @IdRemision
              ORDER BY Descripcion, IdLote",
            new { IdRemision = idRemision })).ToList();
    }

    /// <summary>
    /// Obtiene las piezas de un lote por IdFactura
    /// </summary>
    public async Task<List<LoteRepetidaItem>> ObtenerPiezasPorFacturaAsync(int idFactura)
    {
        using var db = CreateConnection();
        return (await db.QueryAsync<LoteRepetidaItem>(
            @"SELECT TOP 50 Descripcion, IdLote, CodigoBarras, IdRemision, IdFactura,
                     Cantidad, Peso, PrecioGramo, CostoBruto, Descuento, CostoNeto,
                     IdMoneda, FechaCaptura, FechaUltEdicion, IdUsuario,
                     IdTienda, IdLocalizacion, TCCosto, TCCotizacion, Precio, Nombre
              FROM vLotesRepetidas
              WHERE IdFactura = @IdFactura
              ORDER BY Descripcion, IdLote",
            new { IdFactura = idFactura })).ToList();
    }

    /// <summary>
    /// Crea una nueva pieza en el lote. Genera IdLote con el contador.
    /// Fórmula de precio: CostoNeto × Utilidad × UtilidadExtra × Impuesto / Divisor × TCCotizacion
    /// </summary>
    public async Task<int> CrearPiezaEnLoteAsync(CrearLoteRepetidaRequest req, int idTienda)
    {
        using var db = CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();

        try
        {
            // Validar que el código existe en catálogo
            var catalogo = await db.QueryFirstOrDefaultAsync<CatalogoRepetida>(
                "SELECT TOP 1 CodigoBarras FROM catalogorepetidas WHERE CodigoBarras = @Codigo",
                new { Codigo = req.CodigoBarras }, transaction: tx);

            if (catalogo == null)
                throw new InvalidOperationException($"Código de barras '{req.CodigoBarras}' no existe en catálogo de repetidas.");

            // Obtener y actualizar contador de lote
            var contadorLote = await db.ExecuteScalarAsync<int>(
                "SELECT ISNULL(lote, 0) + 1 FROM contador", transaction: tx);
            await db.ExecuteAsync(
                "UPDATE contador SET lote = ISNULL(lote, 0) + 1", transaction: tx);

            var idLote = (idTienda * 100000) + contadorLote;

            await db.ExecuteAsync(
                @"INSERT INTO LotesRepetidas
                    (IdLote, CodigoBarras, IdRemision, IdFactura, Cantidad, Peso,
                     PrecioGramo, CostoBruto, Descuento, CostoNeto, IdMoneda,
                     FechaCaptura, FechaUltEdicion, IdUsuario, IdTienda, IdLocalizacion,
                     TCCosto, TCCotizacion)
                  VALUES
                    (@IdLote, @CodigoBarras, @IdRemision, @IdFactura, @Cantidad, @Peso,
                     @PrecioGramo, @CostoBruto, @Descuento, @CostoNeto, @IdMoneda,
                     GETUTCDATE(), GETUTCDATE(), 1, @IdTienda, @IdTienda,
                     @TCCosto, @TCCotizacion)",
                new
                {
                    IdLote = idLote,
                    req.CodigoBarras,
                    req.IdRemision,
                    req.IdFactura,
                    req.Cantidad,
                    req.Peso,
                    req.PrecioGramo,
                    req.CostoBruto,
                    req.Descuento,
                    req.CostoNeto,
                    req.IdMoneda,
                    IdTienda = idTienda,
                    req.TCCosto,
                    req.TCCotizacion
                }, transaction: tx);

            tx.Commit();
            _logger.LogInformation("Pieza creada en lote: IdLote={Lote}, Código={Codigo}", idLote, req.CodigoBarras);
            return idLote;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Elimina una pieza del lote por CodigoBarras y FechaCaptura
    /// </summary>
    public async Task EliminarPiezaAsync(string codigoBarras, DateTime fechaCaptura)
    {
        using var db = CreateConnection();
        var deleted = await db.ExecuteAsync(
            @"DELETE FROM LotesRepetidas
              WHERE CodigoBarras = @Codigo
                AND DATEADD(s, -2, FechaCaptura) <= @Fecha
                AND DATEADD(s, 2, FechaCaptura) >= @Fecha",
            new { Codigo = codigoBarras, Fecha = fechaCaptura });

        _logger.LogInformation("Pieza eliminada: Código={Codigo}, Fecha={Fecha}, Filas={N}",
            codigoBarras, fechaCaptura, deleted);
    }

    /// <summary>
    /// Obtiene razones sociales de un proveedor
    /// </summary>
    public async Task<List<RazonSocialProveedorItem>> ObtenerRazonesSocialesAsync(int idProveedor)
    {
        using var db = CreateConnection();
        return (await db.QueryAsync<RazonSocialProveedorItem>(
            @"SELECT TOP 50 rsp.IdRazonSocialProveedor, rs.RazonSocialProveedor AS RazonSocial
              FROM Razones_Sociales_Proveedores_Proveedores rsp
              INNER JOIN Razones_Sociales_Proveedores rs
                ON rs.IdRazonSocialProveedor = rsp.IdRazonSocialProveedor
              WHERE rsp.Proveedor = @IdProv",
            new { IdProv = idProveedor })).ToList();
    }
}
