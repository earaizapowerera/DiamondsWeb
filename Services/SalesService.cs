using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

public class SalesService
{
    private readonly string _connectionString;
    private readonly ILogger<SalesService> _logger;

    public SalesService(string connectionString, ILogger<SalesService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    // ══════════════════════════════════════════════
    // CONSULTA DE NOTAS
    // ══════════════════════════════════════════════
    public async Task<List<NotaVenta>> ObtenerNotasAsync(DateTime? desde = null, DateTime? hasta = null,
        string? nombreCliente = null, string? codigoBarras = null)
    {
        using var conn = CreateConnection();
        var sql = @"SELECT bn.IdNota, bn.NombreCliente, bn.FechaBaja, bn.Total, bn.Neto, bn.Bruto,
                       bn.Descuento, bn.IdUsuario,
                       (SELECT COUNT(*) FROM PiezasNotas pn WHERE pn.IdNota = bn.IdNota) AS CantidadPiezas
                    FROM BajasNotas bn WHERE 1=1";
        if (desde.HasValue) sql += " AND bn.FechaBaja >= @Desde";
        if (hasta.HasValue) sql += " AND bn.FechaBaja <= @Hasta";
        if (!string.IsNullOrWhiteSpace(nombreCliente)) sql += " AND bn.NombreCliente LIKE @NC";
        if (!string.IsNullOrWhiteSpace(codigoBarras))
            sql += " AND bn.IdNota IN (SELECT IdNota FROM PiezasNotas WHERE CodigoBarras = @CB)";
        sql += " ORDER BY bn.FechaBaja DESC";
        return (await conn.QueryAsync<NotaVenta>(sql,
            new { Desde = desde, Hasta = hasta, NC = $"%{nombreCliente}%", CB = codigoBarras })).ToList();
    }

    public async Task<List<PiezaNota>> ObtenerPiezasNotaAsync(string idNota)
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<PiezaNota>(@"
            SELECT pn.IdNota, pn.CodigoBarras, pn.Descripcion, pn.SubTotal, pn.Total, pn.Cantidad,
                   p.Precio, prov.NombreProveedor
            FROM PiezasNotas pn
            LEFT JOIN Piezas p ON pn.CodigoBarras = p.CodigoBarras
            LEFT JOIN Proveedores prov ON p.Proveedor = prov.Proveedor
            WHERE pn.IdNota = @IdNota", new { IdNota = idNota })).ToList();
    }

    public async Task<List<PagoNota>> ObtenerPagosNotaAsync(string idNota)
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<PagoNota>(@"
            SELECT pn.IdNota, pn.IdOpcionPago, op.OpcionPago, pn.Importe, pn.ImporteOriginal, pn.TipoCambio
            FROM BajasPagosNotas pn
            LEFT JOIN OpcionesPago op ON pn.IdOpcionPago = op.IdOpcionPago
            WHERE pn.IdNota = @IdNota", new { IdNota = idNota })).ToList();
    }

    public async Task<string> CancelarNotaAsync(string idNota)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync("EXEC restaurarnota @IdNota", new { IdNota = idNota });
        return "Nota cancelada y piezas restauradas al inventario";
    }

    // ══════════════════════════════════════════════
    // CONSULTA DE BAJAS
    // ══════════════════════════════════════════════
    public async Task<List<BajaPieza>> ObtenerBajasPiezasAsync(string? buscar = null)
    {
        using var conn = CreateConnection();
        var sql = @"SELECT bp.CodigoBarras, bp.Descripcion, bp.Precio, prov.NombreProveedor,
                       g.Grupo, bn.NombreCliente, bn.FechaBaja, bp.IdNota
                    FROM vBajasPiezas bp
                    LEFT JOIN Proveedores prov ON bp.Proveedor = prov.Proveedor
                    LEFT JOIN Grupos g ON bp.IdGrupo = g.IdGrupo
                    LEFT JOIN BajasNotas bn ON bp.IdNota = bn.IdNota
                    WHERE 1=1";
        if (!string.IsNullOrWhiteSpace(buscar))
            sql += " AND (bp.CodigoBarras LIKE @B OR bp.Descripcion LIKE @B OR bn.NombreCliente LIKE @B)";
        sql += " ORDER BY bn.FechaBaja DESC";
        return (await conn.QueryAsync<BajaPieza>(sql, new { B = $"%{buscar}%" })).ToList();
    }

    // ══════════════════════════════════════════════
    // DEVOLUCIONES A PROVEEDOR
    // ══════════════════════════════════════════════
    public async Task<List<Devolucion>> ObtenerDevolucionesAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<Devolucion>(@"
            SELECT d.IdDevolucion, d.CodigoBarras, p.Descripcion, d.Motivo, d.Remision, d.IdUsuario, d.FechaCaptura
            FROM Devoluciones d
            LEFT JOIN Piezas p ON d.CodigoBarras = p.CodigoBarras
            ORDER BY d.FechaCaptura DESC")).ToList();
    }

    public async Task<string> CrearDevolucionAsync(string codigoBarras, string motivo, int idUsuario)
    {
        using var conn = CreateConnection();
        var existe = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Piezas WHERE CodigoBarras = @CB", new { CB = codigoBarras });
        if (existe == 0) return "Pieza no encontrada";

        await conn.ExecuteAsync("EXEC sp_devolucion @CB, @Motivo, @IdUsuario",
            new { CB = codigoBarras, Motivo = motivo, IdUsuario = idUsuario });
        return "Devolución registrada";
    }

    public async Task AplicarRemisionDevolucionAsync(int idDevolucion, string remision)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE Devoluciones SET Remision = @R WHERE IdDevolucion = @Id",
            new { R = remision, Id = idDevolucion });
    }

    // ══════════════════════════════════════════════
    // DEVOLUCIONES DE CLIENTE
    // ══════════════════════════════════════════════
    public async Task<DevolucionCliente?> BuscarDevolucionClienteAsync(string codigoBarras)
    {
        using var conn = CreateConnection();
        // Verificar si ya fue reestablecida
        var yaReestablecida = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM PiezasReestablecidas WHERE CodigoBarras = @CB", new { CB = codigoBarras });

        var resultado = await conn.QueryFirstOrDefaultAsync<DevolucionCliente>(@"
            SELECT pn.CodigoBarras, bn.NombreCliente, bn.FechaBaja AS FechaCompra,
                   pn.Descripcion, pn.Total AS Precio, bn.Descuento,
                   pn.Total * (1 - ISNULL(bn.Descuento,0)/100.0) AS PrecioPagado
            FROM PiezasNotas pn
            INNER JOIN BajasNotas bn ON pn.IdNota = bn.IdNota
            WHERE pn.CodigoBarras = @CB", new { CB = codigoBarras });

        if (resultado != null)
            resultado.YaReestablecida = yaReestablecida > 0;

        return resultado;
    }

    public async Task<string> ReestablecerPiezaAsync(string codigoBarras, int idUsuario)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync("EXEC sp_reestablecerpieza @CB", new { CB = codigoBarras });
        return "Pieza reestablecida al inventario";
    }

    // ══════════════════════════════════════════════
    // CONSIGNACIÓN
    // ══════════════════════════════════════════════
    public async Task<List<ConsignacionItem>> ObtenerConsignacionAsync(string? idRemision = null)
    {
        using var conn = CreateConnection();
        var sql = @"SELECT p.CodigoBarras, p.Descripcion, p.Precio, prov.NombreProveedor,
                       p.IdRemision,
                       CASE
                           WHEN p.Faltante = 1 THEN 'Devuelto'
                           WHEN p.IdStatus = 2 THEN 'Por Devolver'
                           ELSE 'En Existencia'
                       END AS Estado
                    FROM Piezas p
                    INNER JOIN Remisiones r ON p.IdRemision = r.IdRemision
                    LEFT JOIN Proveedores prov ON p.Proveedor = prov.Proveedor
                    WHERE r.Consignacion = 1";
        if (!string.IsNullOrWhiteSpace(idRemision))
            sql += " AND p.IdRemision = @IdRem";
        sql += " ORDER BY p.CodigoBarras";
        return (await conn.QueryAsync<ConsignacionItem>(sql, new { IdRem = idRemision })).ToList();
    }

    // ══════════════════════════════════════════════
    // ACTUALIZACIÓN DESDE FACTURAS
    // ══════════════════════════════════════════════
    public async Task<List<Factura>> ObtenerFacturasAsync(int? proveedor = null)
    {
        using var conn = CreateConnection();
        var sql = @"SELECT f.IdFactura, f.FolioFactura, f.Proveedor, prov.NombreProveedor,
                       f.IdRazonSocial, rs.RazonSocialProveedor AS RazonSocial,
                       f.FechaFactura
                    FROM Facturas f
                    LEFT JOIN Proveedores prov ON f.Proveedor = prov.Proveedor
                    LEFT JOIN Razones_Sociales_Proveedores rs ON f.IdRazonSocial = rs.IdRazonSocialProveedor
                    WHERE 1=1";
        if (proveedor.HasValue) sql += " AND f.Proveedor = @Prov";
        sql += " ORDER BY f.FechaFactura DESC";
        return (await conn.QueryAsync<Factura>(sql, new { Prov = proveedor })).ToList();
    }

    public async Task<List<PiezaActualizable>> ObtenerPiezasParaActualizarAsync(string? idFactura = null)
    {
        using var conn = CreateConnection();
        var sql = @"SELECT CodigoBarras, Descripcion, Precio, NombreProveedor, IdFactura, IdRemision,
                       CBPieza, CNPieza, CBFactura, CNFactura, TCCosto, IdMoneda
                    FROM vActualizaPiezas WHERE 1=1";
        if (!string.IsNullOrWhiteSpace(idFactura))
            sql += " AND (IdFactura IS NULL OR IdFactura = @IdFact)";
        sql += " ORDER BY CodigoBarras";
        return (await conn.QueryAsync<PiezaActualizable>(sql, new { IdFact = idFactura })).ToList();
    }

    public async Task AsignarPiezaFacturaAsync(string codigoBarras, string idFactura, decimal? tcCosto,
        decimal? cbFactura, decimal? cnFactura)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE Piezas SET IdFactura = @IdFact, TCCosto = @TC, CBFactura = @CB, CNFactura = @CN,
                FechaUltEdicion = GETDATE()
            WHERE CodigoBarras = @Codigo",
            new { Codigo = codigoBarras, IdFact = idFactura, TC = tcCosto, CB = cbFactura, CN = cnFactura });
    }

    public async Task DesasignarPiezaFacturaAsync(string codigoBarras)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE Piezas SET IdFactura = NULL, CBFactura = NULL, CNFactura = NULL, TCCosto = NULL,
                FechaUltEdicion = GETDATE()
            WHERE CodigoBarras = @Codigo", new { Codigo = codigoBarras });
    }

    // ══════════════════════════════════════════════
    // ACTUALIZACIÓN DE REMISIONES
    // ══════════════════════════════════════════════
    public async Task<List<Remision>> ObtenerRemisionesAsync(int? proveedor = null)
    {
        using var conn = CreateConnection();
        var sql = @"SELECT r.IdRemision, r.Proveedor, prov.NombreProveedor,
                       r.NumRemision, r.FechaRemision, r.Consignacion
                    FROM Remisiones r
                    LEFT JOIN Proveedores prov ON r.Proveedor = prov.Proveedor
                    WHERE 1=1";
        if (proveedor.HasValue) sql += " AND r.Proveedor = @Prov";
        sql += " ORDER BY r.FechaRemision DESC";
        return (await conn.QueryAsync<Remision>(sql, new { Prov = proveedor })).ToList();
    }

    public async Task AsignarPiezaRemisionAsync(string codigoBarras, string idRemision)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE Piezas SET IdRemision = @IdRem, FechaUltEdicion = GETDATE()
            WHERE CodigoBarras = @Codigo", new { Codigo = codigoBarras, IdRem = idRemision });
    }

    // ══════════════════════════════════════════════
    // POS — PUNTO DE VENTA
    // ══════════════════════════════════════════════
    public async Task<string> CrearSesionVentaAsync(int idUsuario)
    {
        using var conn = CreateConnection();
        var nota = await conn.ExecuteScalarAsync<int>("SELECT ISNULL(Nota,0)+1 FROM contador");
        await conn.ExecuteAsync("UPDATE contador SET Nota = Nota + 1");
        var idTienda = await conn.ExecuteScalarAsync<int?>("SELECT TOP 1 IdTienda FROM Tiendas") ?? 1;
        var idNota = $"{idTienda}{nota:D6}";
        return idNota;
    }

    public async Task<string?> BuscarPiezaParaVentaAsync(string codigoBarras)
    {
        using var conn = CreateConnection();
        // Buscar en Etiquetas/Piezas
        var pieza = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT p.CodigoBarras, p.Descripcion, p.Precio, p.IdDivisor, d.Divisor
            FROM Piezas p
            LEFT JOIN Divisores d ON p.IdDivisor = d.IdDivisor
            WHERE p.CodigoBarras = @CB AND p.CBPadre IS NULL", new { CB = codigoBarras });
        if (pieza != null) return pieza.CodigoBarras;

        // Buscar en CatalogoRepetidas
        var repetida = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT CodigoBarras FROM CatalogoRepetidas WHERE CodigoBarras = @CB", new { CB = codigoBarras });
        if (repetida != null) return repetida.CodigoBarras;

        // Buscar en Compuestas
        var compuesta = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT CodigoBarras FROM Compuestas WHERE CodigoBarras = @CB", new { CB = codigoBarras });
        if (compuesta != null) return compuesta.CodigoBarras;

        return null;
    }

    public async Task AgregarPiezaVentaAsync(string idNota, string codigoBarras, string descripcion,
        decimal subTotal, decimal total, int cantidad = 1)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(@"
            INSERT INTO PiezasNotasTemporal (IdNota, CodigoBarras, Descripcion, SubTotal, Cantidad, Total)
            VALUES (@IdNota, @CB, @Desc, @SubTotal, @Cant, @Total)",
            new { IdNota = idNota, CB = codigoBarras, Desc = descripcion, SubTotal = subTotal, Cant = cantidad, Total = total });
    }

    public async Task<List<PiezaNotaTemporal>> ObtenerPiezasVentaAsync(string idNota)
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<PiezaNotaTemporal>(@"
            SELECT IdNota, CodigoBarras, Descripcion, SubTotal, Cantidad, Total
            FROM PiezasNotasTemporal WHERE IdNota = @IdNota", new { IdNota = idNota })).ToList();
    }

    public async Task EliminarPiezaVentaAsync(string idNota, string codigoBarras)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM PiezasNotasTemporal WHERE IdNota = @IdNota AND CodigoBarras = @CB",
            new { IdNota = idNota, CB = codigoBarras });
    }

    public async Task AgregarPagoAsync(string idNota, int idOpcionPago, decimal importe,
        decimal? importeOriginal = null, decimal? tipoCambio = null)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(@"
            INSERT INTO PagosNotas (IdNota, IdOpcionPago, Importe, ImporteOriginal, TipoCambio)
            VALUES (@IdNota, @IdOP, @Importe, @ImporteOrig, @TC)",
            new { IdNota = idNota, IdOP = idOpcionPago, Importe = importe, ImporteOrig = importeOriginal, TC = tipoCambio });
    }

    public async Task<List<PagoNotaTemporal>> ObtenerPagosVentaAsync(string idNota)
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<PagoNotaTemporal>(@"
            SELECT pn.IdNota, pn.IdOpcionPago, op.OpcionPago AS NombreOpcionPago,
                   pn.Importe, pn.ImporteOriginal, pn.TipoCambio
            FROM PagosNotas pn
            LEFT JOIN OpcionesPago op ON pn.IdOpcionPago = op.IdOpcionPago
            WHERE pn.IdNota = @IdNota", new { IdNota = idNota })).ToList();
    }

    public async Task EliminarPagoAsync(string idNota, int idOpcionPago, decimal importe)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(@"
            DELETE TOP(1) FROM PagosNotas
            WHERE IdNota = @IdNota AND IdOpcionPago = @IdOP AND Importe = @Importe",
            new { IdNota = idNota, IdOP = idOpcionPago, Importe = importe });
    }

    public async Task<string> CerrarNotaAsync(string idNota, string nombreCliente, DateTime fechaBaja, int idUsuario)
    {
        using var conn = CreateConnection();
        // Ejecutar SP de dar de baja
        await conn.ExecuteAsync("EXEC sp_DardeBaja @IdNota, @NombreCliente, @FechaBaja, @IdUsuario",
            new { IdNota = idNota, NombreCliente = nombreCliente, FechaBaja = fechaBaja, IdUsuario = idUsuario });

        // Limpiar temporales
        await conn.ExecuteAsync("DELETE FROM PiezasNotasTemporal WHERE IdNota = @IdNota", new { IdNota = idNota });
        await conn.ExecuteAsync("DELETE FROM PagosNotas WHERE IdNota = @IdNota", new { IdNota = idNota });

        return $"Nota {idNota} cerrada exitosamente";
    }

    public async Task<List<OpcionPago>> ObtenerOpcionesPagoActivasAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<OpcionPago>(@"
            SELECT op.IdOpcionPago, op.OpcionPago AS OpcionPago1, op.IdMoneda, m.Moneda, op.IdLogo
            FROM OpcionesPago op
            LEFT JOIN Monedas m ON op.IdMoneda = m.IdMoneda
            WHERE op.Activo = 1 ORDER BY op.OpcionPago")).ToList();
    }
}
