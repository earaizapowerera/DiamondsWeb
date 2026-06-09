using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

public class ActualizacionService
{
    private readonly string _connectionString;
    private readonly ILogger<ActualizacionService> _logger;

    public ActualizacionService(string connectionString, ILogger<ActualizacionService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    // ══════════════════════════════════════════════
    // BÚSQUEDA DE PIEZAS (vista vactualizapiezas)
    // ══════════════════════════════════════════════
    public async Task<List<PiezaActualizacion>> BuscarPiezasAsync(string buscar)
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<PiezaActualizacion>(@"
            SELECT TOP 100 CodigoBarras, Obs2, IdFactura, IdRemision, Remision,
                   Proveedor, Descripcion, FechaCaptura, TCCosto,
                   CBPieza, CNPieza, DescPieza,
                   CostoMN, IdMoneda, CostoBrutoMN
            FROM vactualizapiezas
            WHERE CodigoBarras LIKE @B
               OR Descripcion LIKE @B
               OR Obs2 LIKE @B
               OR Remision LIKE @B
            ORDER BY FechaCaptura DESC",
            new { B = $"%{buscar}%" })).ToList();
    }

    // ══════════════════════════════════════════════
    // BÚSQUEDA DE FACTURAS
    // ══════════════════════════════════════════════
    public async Task<FacturaBusqueda?> BuscarFacturaPorFolioYProveedorAsync(
        string folioFactura, int proveedor)
    {
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<FacturaBusqueda>(@"
            SELECT f.IdFactura, f.FolioFactura, f.FechaFactura,
                   f.FechaCaptura, f.FechaUltEdicion, f.IdUsuario,
                   rsp.RazonSocialProveedor, f.IdRazonSocialProveedor, f.Proveedor
            FROM facturas f
            INNER JOIN razones_sociales_proveedores rsp
                ON rsp.IdRazonSocialProveedor = f.IdRazonSocialProveedor
            WHERE (f.FolioFactura LIKE @FolioConPrefijo OR f.FolioFactura = @Folio)
              AND f.Proveedor = @Proveedor",
            new
            {
                FolioConPrefijo = $"F-{folioFactura}",
                Folio = folioFactura,
                Proveedor = proveedor
            });
    }

    public async Task<FacturaBusqueda?> ObtenerFacturaPorIdAsync(int idFactura)
    {
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<FacturaBusqueda>(@"
            SELECT f.IdFactura, f.FolioFactura, f.FechaFactura,
                   f.FechaCaptura, f.FechaUltEdicion, f.IdUsuario,
                   rsp.RazonSocialProveedor, f.IdRazonSocialProveedor, f.Proveedor
            FROM facturas f
            INNER JOIN razones_sociales_proveedores rsp
                ON rsp.IdRazonSocialProveedor = f.IdRazonSocialProveedor
            WHERE f.IdFactura = @Id",
            new { Id = idFactura });
    }

    public async Task<string?> ObtenerFolioFacturaAsync(int idFactura)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<string?>(
            "SELECT FolioFactura FROM facturas WHERE IdFactura = @Id",
            new { Id = idFactura });
    }

    // ══════════════════════════════════════════════
    // CREAR FACTURA
    // ══════════════════════════════════════════════
    public async Task<int> CrearFacturaAsync(
        string folioFactura, int proveedor, int idRazonSocial,
        DateTime fechaFactura, int idUsuario, int idTienda)
    {
        using var conn = CreateConnection();
        conn.Open();

        // Generar siguiente ID desde contador (igual que VB6)
        var currentId = await conn.ExecuteScalarAsync<int>(
            "SELECT ISNULL(factura, 0) FROM contador");
        if (currentId == 0)
            await conn.ExecuteAsync("UPDATE contador SET factura = 1");
        else
            await conn.ExecuteAsync("UPDATE contador SET factura = factura + 1");

        var newId = (currentId == 0 ? 1 : currentId + 1);
        var idFactura = idTienda * 100000 + newId; // formato IdTienda + secuencial

        await conn.ExecuteAsync(@"
            INSERT INTO facturas (IdFactura, FolioFactura, Proveedor,
                IdRazonSocialProveedor, FechaFactura, FechaCaptura,
                FechaUltEdicion, IdUsuario, IdTienda)
            VALUES (@IdFactura, @FolioFactura, @Proveedor,
                @IdRazonSocial, @FechaFactura, GETUTCDATE(),
                GETUTCDATE(), @IdUsuario, @IdTienda)",
            new
            {
                IdFactura = idFactura,
                FolioFactura = folioFactura,
                Proveedor = proveedor,
                IdRazonSocial = idRazonSocial,
                FechaFactura = fechaFactura,
                IdUsuario = idUsuario,
                IdTienda = idTienda
            });

        _logger.LogInformation(
            "Factura creada: IdFactura={Id}, Folio={Folio}, Proveedor={Prov}",
            idFactura, folioFactura, proveedor);

        return idFactura;
    }

    // ══════════════════════════════════════════════
    // ACTUALIZAR COSTOS DE PIEZA
    // ══════════════════════════════════════════════
    public async Task ActualizarCostosPiezaAsync(ActualizarCostoPiezaDto dto)
    {
        using var conn = CreateConnection();
        var rows = await conn.ExecuteAsync(@"
            UPDATE piezas
            SET IdFactura = @IdFactura,
                CBPieza = @CBPieza,
                CNPieza = @CNPieza,
                IdMoneda = @IdMoneda,
                TCCosto = @TCCosto,
                CBFactura = @CBFactura,
                CNFactura = @CNFactura,
                DescFactura = @DescFactura
            WHERE CodigoBarras = @CodigoBarras",
            new
            {
                dto.CodigoBarras,
                dto.IdFactura,
                dto.CBPieza,
                dto.CNPieza,
                dto.IdMoneda,
                dto.TCCosto,
                dto.CBFactura,
                dto.CNFactura,
                dto.DescFactura
            });

        _logger.LogInformation(
            "Costos actualizados: CB={CB}, IdFactura={Fac}, CBPieza={CBP}, CNPieza={CNP}, Moneda={Mon}, TC={TC}",
            dto.CodigoBarras, dto.IdFactura, dto.CBPieza, dto.CNPieza, dto.IdMoneda, dto.TCCosto);

        if (rows == 0)
            throw new InvalidOperationException(
                $"No se encontró pieza con código de barras '{dto.CodigoBarras}'");
    }

    // ══════════════════════════════════════════════
    // CATÁLOGOS DE APOYO
    // ══════════════════════════════════════════════
    public async Task<List<MonedaCatalogo>> ObtenerMonedasAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<MonedaCatalogo>(
            "SELECT IdMoneda, Moneda, Extranjera FROM monedas ORDER BY IdMoneda"
        )).ToList();
    }

    public async Task<List<RazonSocialCatalogo>> ObtenerRazonesSocialesPorProveedorAsync(
        int proveedor)
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<RazonSocialCatalogo>(@"
            SELECT rsp.IdRazonSocialProveedor, rsp.RazonSocialProveedor, rspp.Proveedor
            FROM razones_sociales_proveedores rsp
            INNER JOIN razones_sociales_proveedores_proveedores rspp
                ON rspp.IdRazonSocialProveedor = rsp.IdRazonSocialProveedor
            WHERE rspp.Proveedor = @Prov
            ORDER BY rsp.RazonSocialProveedor",
            new { Prov = proveedor })).ToList();
    }
}
