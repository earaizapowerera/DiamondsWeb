using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio CRUD para catálogos de Diamonds (DefaultsUtilidad, etc.)
/// </summary>
public class CatalogService
{
    private readonly string _connectionString;
    private readonly ILogger<CatalogService> _logger;

    public CatalogService(string connectionString, ILogger<CatalogService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    // ── DEFAULTS UTILIDAD ────────────────────────────────────────

    public async Task<List<DefaultUtilidad>> ObtenerDefaultsUtilidadAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<DefaultUtilidad>(
            @"SELECT d.IdDefaultUtilidad,
                     d.DefaultUtilidad AS DefaultUtilidadGeneral,
                     d.DefaultUtilidadGemas,
                     d.DefaultUtilidadReloj,
                     d.IdUsuario,
                     u.Nombre AS NombreUsuario,
                     d.FechaCaptura
              FROM DefaultsUtilidad d
              INNER JOIN Usuarios u ON u.IdUsuario = d.IdUsuario
              WHERE d.IdDefaultUtilidad > 0
              ORDER BY d.FechaCaptura DESC"
        )).ToList();
    }

    public async Task<DefaultUtilidad?> ObtenerDefaultUtilidadAsync(int id)
    {
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<DefaultUtilidad>(
            @"SELECT d.IdDefaultUtilidad,
                     d.DefaultUtilidad AS DefaultUtilidadGeneral,
                     d.DefaultUtilidadGemas,
                     d.DefaultUtilidadReloj,
                     d.IdUsuario,
                     u.Nombre AS NombreUsuario,
                     d.FechaCaptura
              FROM DefaultsUtilidad d
              INNER JOIN Usuarios u ON u.IdUsuario = d.IdUsuario
              WHERE d.IdDefaultUtilidad = @Id",
            new { Id = id });
    }

    public async Task<int> CrearDefaultUtilidadAsync(
        decimal utilidad, decimal? utilidadReloj, decimal? utilidadGemas, int idUsuario)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO DefaultsUtilidad
                (DefaultUtilidad, DefaultUtilidadGemas, DefaultUtilidadReloj, IdUsuario, FechaCaptura)
              OUTPUT INSERTED.IdDefaultUtilidad
              VALUES (@Utilidad, @UtilidadGemas, @UtilidadReloj, @IdUsuario, GETUTCDATE())",
            new
            {
                Utilidad = utilidad,
                UtilidadGemas = utilidadGemas ?? 0m,
                UtilidadReloj = utilidadReloj ?? 0m,
                IdUsuario = idUsuario
            });
    }

    public async Task ActualizarDefaultUtilidadAsync(
        int id, decimal utilidad, decimal utilidadGemas, decimal utilidadReloj, int idUsuario)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            @"UPDATE DefaultsUtilidad
              SET DefaultUtilidad = @Utilidad,
                  DefaultUtilidadGemas = @UtilidadGemas,
                  DefaultUtilidadReloj = @UtilidadReloj,
                  IdUsuario = @IdUsuario
              WHERE IdDefaultUtilidad = @Id",
            new
            {
                Id = id,
                Utilidad = utilidad,
                UtilidadGemas = utilidadGemas,
                UtilidadReloj = utilidadReloj,
                IdUsuario = idUsuario
            });
    }

    public async Task EliminarDefaultUtilidadAsync(int id)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync("DELETE FROM DefaultsUtilidad WHERE IdDefaultUtilidad = @Id", new { Id = id });
    }

    // ══════════════════════════════════════════════
    // DEFAULTS UTILIDAD EXTRA
    // ══════════════════════════════════════════════
    public async Task<List<DefaultUtilidadExtra>> ObtenerDefaultsUtilidadExtraAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<DefaultUtilidadExtra>(@"
            SELECT IdDefaultUtilidadExtra, DefaultUtilidadExtra AS DefaultUtilidadExtra1, IdUsuario, FechaCaptura
            FROM DefaultsUtilidadExtra ORDER BY FechaCaptura DESC")).ToList();
    }

    public async Task<int> CrearDefaultUtilidadExtraAsync(decimal utilidadExtra, int idUsuario)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO DefaultsUtilidadExtra (DefaultUtilidadExtra, IdUsuario, FechaCaptura) OUTPUT INSERTED.IdDefaultUtilidadExtra VALUES (@UtilidadExtra, @IdUsuario, GETUTCDATE())",
            new { UtilidadExtra = utilidadExtra, IdUsuario = idUsuario });
    }

    public async Task ActualizarDefaultUtilidadExtraAsync(int id, decimal utilidadExtra, int idUsuario)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            @"UPDATE DefaultsUtilidadExtra
              SET DefaultUtilidadExtra = @UtilidadExtra, IdUsuario = @IdUsuario, FechaCaptura = GETUTCDATE()
              WHERE IdDefaultUtilidadExtra = @Id",
            new { Id = id, UtilidadExtra = utilidadExtra, IdUsuario = idUsuario });
    }

    public async Task EliminarDefaultUtilidadExtraAsync(int id)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync("DELETE FROM DefaultsUtilidadExtra WHERE IdDefaultUtilidadExtra = @Id", new { Id = id });
    }

    // ══════════════════════════════════════════════
    // UTILIDAD EXTRA POR PRECIO/GRAMO
    // Tabla real: Id, PrecioGramoDesde, PrecioGramoHasta, DefaultUtilidadExtra, FechaCaptura, IdUsuario, rowguid
    // ══════════════════════════════════════════════
    public async Task<List<UtilidadExtraPrecioGramo>> ObtenerUtilidadExtraPrecioGramoAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<UtilidadExtraPrecioGramo>(@"
            SELECT Id AS IdUtilidadExtra, PrecioGramoDesde, PrecioGramoHasta,
                   DefaultUtilidadExtra AS UtilidadExtra, IdUsuario, FechaCaptura
            FROM UtilidadExtra_PrecioGramo ORDER BY PrecioGramoDesde")).ToList();
    }

    public async Task<int> CrearUtilidadExtraPrecioGramoAsync(decimal desde, decimal hasta, decimal utilidad, int idUsuario)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO UtilidadExtra_PrecioGramo (PrecioGramoDesde, PrecioGramoHasta, DefaultUtilidadExtra, IdUsuario, FechaCaptura)
            OUTPUT INSERTED.Id
            VALUES (@Desde, @Hasta, @Utilidad, @IdUsuario, GETUTCDATE())",
            new { Desde = desde, Hasta = hasta, Utilidad = utilidad, IdUsuario = idUsuario });
    }

    public async Task ActualizarUtilidadExtraPrecioGramoAsync(int id, decimal desde, decimal hasta, decimal utilidad)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE UtilidadExtra_PrecioGramo
            SET PrecioGramoDesde = @Desde, PrecioGramoHasta = @Hasta, DefaultUtilidadExtra = @Utilidad
            WHERE Id = @Id",
            new { Id = id, Desde = desde, Hasta = hasta, Utilidad = utilidad });
    }

    public async Task EliminarUtilidadExtraPrecioGramoAsync(int id)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync("DELETE FROM UtilidadExtra_PrecioGramo WHERE Id = @Id", new { Id = id });
    }

    public async Task<bool> ExisteRangoSolapadoAsync(decimal desde, decimal hasta, int? excluirId = null)
    {
        using var conn = CreateConnection();
        var sql = @"SELECT TOP 1 1 FROM UtilidadExtra_PrecioGramo
                    WHERE PrecioGramoDesde < @Hasta AND PrecioGramoHasta > @Desde";
        if (excluirId.HasValue)
            sql += " AND Id <> @ExcluirId";
        var result = await conn.QueryFirstOrDefaultAsync<int?>(sql,
            new { Desde = desde, Hasta = hasta, ExcluirId = excluirId });
        return result.HasValue;
    }

    // ══════════════════════════════════════════════
    // TABLAS DE JERARQUÍAS
    // ══════════════════════════════════════════════
    public async Task<List<TablaJerarquia>> ObtenerTablasJerarquiasAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<TablaJerarquia>(
            "SELECT IdTabla, Descripcion, IdUsuario FROM TablasJerarquias ORDER BY Descripcion")).ToList();
    }

    public async Task<List<Jerarquia>> ObtenerJerarquiasAsync(int idTabla)
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<Jerarquia>(
            "SELECT IdJerarquia, IdTabla, Columna, Orden FROM Jerarquias WHERE IdTabla = @Id ORDER BY IdJerarquia",
            new { Id = idTabla })).ToList();
    }

    public async Task<int> CrearTablaJerarquiaAsync(string descripcion, int idUsuario)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO TablasJerarquias (Descripcion, IdUsuario) OUTPUT INSERTED.IdTabla VALUES (@Desc, @IdUsuario)",
            new { Desc = descripcion, IdUsuario = idUsuario });
    }

    public async Task ActualizarTablaJerarquiaAsync(int id, string descripcion, int idUsuario)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE TablasJerarquias SET Descripcion = @Desc, IdUsuario = @IdUsuario WHERE IdTabla = @Id",
            new { Id = id, Desc = descripcion, IdUsuario = idUsuario });
    }

    public async Task EliminarTablaJerarquiaAsync(int id)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync("DELETE FROM Jerarquias WHERE IdTabla = @Id", new { Id = id });
        await conn.ExecuteAsync("DELETE FROM TablasJerarquias WHERE IdTabla = @Id", new { Id = id });
    }

    public async Task<int> CrearJerarquiaAsync(int idTabla, string columna, int orden)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO Jerarquias (IdTabla, Columna, Orden) OUTPUT INSERTED.IdJerarquia VALUES (@IdTabla, @Columna, @Orden)",
            new { IdTabla = idTabla, Columna = columna, Orden = orden });
    }

    public async Task ActualizarJerarquiaAsync(int id, string columna, int orden)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE Jerarquias SET Columna = @Columna, Orden = @Orden WHERE IdJerarquia = @Id",
            new { Id = id, Columna = columna, Orden = orden });
    }

    public async Task EliminarJerarquiaAsync(int id)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync("DELETE FROM Jerarquias WHERE IdJerarquia = @Id", new { Id = id });
    }

    // ══════════════════════════════════════════════
    // DISEÑO ETIQUETAS
    // ══════════════════════════════════════════════
    public async Task<List<DisenioEtiqueta>> ObtenerDiseniosEtiquetasAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<DisenioEtiqueta>(
            "SELECT IdDisenio, Descripcion, ArchivoEtiqueta, ArchivoEtiquetaCompuesta FROM DisenosEtiquetas ORDER BY Descripcion")).ToList();
    }

    // ══════════════════════════════════════════════
    // DIAMANTES (vista vdiamantes)
    // ══════════════════════════════════════════════
    public async Task<List<DiamanteLista>> ObtenerDiamantesAsync(string? buscar = null)
    {
        using var conn = CreateConnection();
        var sql = @"SELECT TOP 500 CodigoBarras, Descripcion, Quilates, Color, Pureza, Corte, Obs1, Obs2, Precio, NombreProveedor
                    FROM vdiamantes
                    WHERE 1=1";
        if (!string.IsNullOrWhiteSpace(buscar))
            sql += " AND (Descripcion LIKE @B OR CodigoBarras LIKE @B OR Color LIKE @B OR Pureza LIKE @B)";
        sql += " ORDER BY CodigoBarras";
        return (await conn.QueryAsync<DiamanteLista>(sql, new { B = $"%{buscar}%" })).ToList();
    }

    // ══════════════════════════════════════════════
    // DEFAULTS FACTOR COMUNES
    // ══════════════════════════════════════════════

    public async Task<List<DefaultFactorComun>> ObtenerDefaultsFactorComunesAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<DefaultFactorComun>(
            @"SELECT Id AS IdDefault, DefaultImpuesto, DefaultDivisor, IdUsuario, FechaCaptura
              FROM DefaultsFactorComunes
              ORDER BY FechaCaptura DESC")).ToList();
    }

    public async Task<int> CrearDefaultFactorComunAsync(decimal impuesto, decimal divisor, int idUsuario)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO DefaultsFactorComunes (DefaultImpuesto, DefaultDivisor, IdUsuario, FechaCaptura)
              OUTPUT INSERTED.Id
              VALUES (@Impuesto, @Divisor, @IdUsuario, GETUTCDATE())",
            new { Impuesto = impuesto, Divisor = divisor, IdUsuario = idUsuario });
    }

    public async Task ActualizarDefaultFactorComunAsync(int id, decimal impuesto, decimal divisor, int idUsuario)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            @"UPDATE DefaultsFactorComunes
              SET DefaultImpuesto = @Impuesto, DefaultDivisor = @Divisor, IdUsuario = @IdUsuario
              WHERE Id = @Id",
            new { Id = id, Impuesto = impuesto, Divisor = divisor, IdUsuario = idUsuario });
    }

    public async Task EliminarDefaultFactorComunAsync(int id)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync("DELETE FROM DefaultsFactorComunes WHERE Id = @Id", new { Id = id });
    }

    // ══════════════════════════════════════════════
    // DIVISORES (catálogo)
    // ══════════════════════════════════════════════

    public async Task<List<Divisor>> ObtenerDivisoresAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<Divisor>(
            @"SELECT IdDivisor, Divisor AS ValorDivisor, Descripcion
              FROM Divisores ORDER BY Descripcion")).ToList();
    }

    public async Task CrearDivisorAsync(string descripcion, decimal valor)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            @"INSERT INTO Divisores (Divisor, Descripcion, FechaCaptura, FechaUltEdicion, IdUsuario)
              VALUES (@Valor, @Descripcion, GETUTCDATE(), GETUTCDATE(), 1)",
            new { Valor = valor, Descripcion = descripcion });
    }

    public async Task ActualizarDivisorAsync(int id, string descripcion, decimal valor)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            @"UPDATE Divisores SET Divisor = @Valor, Descripcion = @Descripcion, FechaUltEdicion = GETUTCDATE()
              WHERE IdDivisor = @Id",
            new { Id = id, Valor = valor, Descripcion = descripcion });
    }

    public async Task EliminarDivisorAsync(int id)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync("DELETE FROM Divisores WHERE IdDivisor = @Id", new { Id = id });
    }

    // ══════════════════════════════════════════════
    // PROVEEDORES (catálogo)
    // ══════════════════════════════════════════════

    public async Task<List<Proveedor>> ObtenerProveedoresAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<Proveedor>(
            @"SELECT TOP 500 Proveedor AS Proveedor1, NombreProveedor, Telefono, Contacto, Direccion,
                     IdDefaultCaracteristica, IdDefaultTipoCosto, IdDefaultUtilidad, IdMoneda,
                     MonedaDefault, UtilidadExtraPrecioGramo, IdUsuario, FechaCaptura
              FROM Proveedores ORDER BY NombreProveedor")).ToList();
    }

    public async Task CrearProveedorAsync(Proveedor proveedor)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            @"INSERT INTO Proveedores (NombreProveedor, Telefono, Contacto, Direccion,
                     IdDefaultCaracteristica, IdDefaultTipoCosto, IdDefaultUtilidad, IdMoneda,
                     MonedaDefault, UtilidadExtraPrecioGramo, IdUsuario, FechaCaptura)
              VALUES (@NombreProveedor, @Telefono, @Contacto, @Direccion,
                     @IdDefaultCaracteristica, @IdDefaultTipoCosto, @IdDefaultUtilidad, @IdMoneda,
                     @MonedaDefault, @UtilidadExtraPrecioGramo, @IdUsuario, GETUTCDATE())",
            proveedor);
    }

    public async Task ActualizarProveedorAsync(Proveedor proveedor)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            @"UPDATE Proveedores SET NombreProveedor = @NombreProveedor, Telefono = @Telefono,
                     Contacto = @Contacto, Direccion = @Direccion,
                     IdDefaultCaracteristica = @IdDefaultCaracteristica, IdDefaultTipoCosto = @IdDefaultTipoCosto,
                     IdDefaultUtilidad = @IdDefaultUtilidad, IdMoneda = @IdMoneda,
                     MonedaDefault = @MonedaDefault, UtilidadExtraPrecioGramo = @UtilidadExtraPrecioGramo,
                     IdUsuario = @IdUsuario
              WHERE Proveedor = @Proveedor1",
            proveedor);
    }

    public async Task EliminarProveedorAsync(int id)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync("DELETE FROM Proveedores WHERE Proveedor = @Id", new { Id = id });
    }

    // ══════════════════════════════════════════════
    // MONEDAS (catálogo)
    // ══════════════════════════════════════════════

    public async Task<List<Moneda>> ObtenerMonedasAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<Moneda>(
            "SELECT IdMoneda, Moneda AS NombreMoneda, Extranjera FROM Monedas ORDER BY Moneda")).ToList();
    }

    // ══════════════════════════════════════════════
    // GRUPOS (catálogo)
    // ══════════════════════════════════════════════

    public async Task<List<Grupo>> ObtenerGruposAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<Grupo>(
            "SELECT IdGrupo, Grupo AS Grupo1 FROM Grupos ORDER BY Grupo")).ToList();
    }

    // ══════════════════════════════════════════════
    // CATALOGO REPETIDAS
    // ══════════════════════════════════════════════

    public async Task<List<CatalogoRepetida>> ObtenerCatalogoRepetidasAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<CatalogoRepetida>(
            @"SELECT TOP 500 cr.CodigoBarras, cr.Descripcion, cr.Proveedor, cr.IdGrupo,
                     cr.Kilates, cr.Precio, cr.IdDivisor, cr.IdUsuario, cr.FechaCaptura,
                     p.NombreProveedor, g.Grupo
              FROM CatalogoRepetidas cr
              LEFT JOIN Proveedores p ON p.Proveedor = cr.Proveedor
              LEFT JOIN Grupos g ON g.IdGrupo = cr.IdGrupo
              ORDER BY cr.Descripcion")).ToList();
    }

    public async Task CrearCatalogoRepetidaAsync(CatalogoRepetida item)
    {
        using var conn = CreateConnection();
        // Generate next barcode from contador
        var nextCB = await conn.ExecuteScalarAsync<int>(
            "SELECT ISNULL(CodigoBarrasRepetida, 0) + 1 FROM contador");
        await conn.ExecuteAsync("UPDATE contador SET CodigoBarrasRepetida = @Next", new { Next = nextCB });
        item.CodigoBarras = nextCB.ToString().PadLeft(7, '0');

        await conn.ExecuteAsync(
            @"INSERT INTO CatalogoRepetidas (CodigoBarras, Descripcion, Proveedor, IdGrupo, Kilates, Precio, IdDivisor, IdUsuario, FechaCaptura)
              VALUES (@CodigoBarras, @Descripcion, @Proveedor, @IdGrupo, @Kilates, @Precio, @IdDivisor, @IdUsuario, GETUTCDATE())",
            item);
    }

    public async Task ActualizarCatalogoRepetidaAsync(CatalogoRepetida item)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            @"UPDATE CatalogoRepetidas SET Descripcion = @Descripcion, Proveedor = @Proveedor,
                     IdGrupo = @IdGrupo, Kilates = @Kilates, Precio = @Precio, IdDivisor = @IdDivisor
              WHERE CodigoBarras = @CodigoBarras",
            item);
    }

    public async Task EliminarCatalogoRepetidaAsync(string codigoBarras)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync("DELETE FROM CatalogoRepetidas WHERE CodigoBarras = @CB", new { CB = codigoBarras });
    }

    // ══════════════════════════════════════════════
    // TIPOS DE CAMBIO
    // ══════════════════════════════════════════════

    public async Task<List<TipoCambio>> ObtenerTiposCambioAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<TipoCambio>(
            @"SELECT TOP 500 tc.IdTipoCambio, tc.IdMoneda, tc.TipoCambioCotizacion,
                     ISNULL(tc.TipoCambioVenta, 0) AS TipoCambioVenta,
                     m.Moneda, tc.IdUsuario, tc.FechaCaptura
              FROM tiposcambio tc
              LEFT JOIN Monedas m ON m.IdMoneda = tc.IdMoneda
              ORDER BY tc.FechaCaptura DESC")).ToList();
    }

    public async Task CrearTipoCambioAsync(int idMoneda, decimal cotizacion, decimal venta, int idUsuario)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            @"INSERT INTO tiposcambio (IdMoneda, TipoCambioCotizacion, TipoCambioVenta, IdUsuario, FechaCaptura)
              VALUES (@IdMoneda, @Cotizacion, @Venta, @IdUsuario, GETUTCDATE())",
            new { IdMoneda = idMoneda, Cotizacion = cotizacion, Venta = venta, IdUsuario = idUsuario });
    }

    public async Task EliminarTipoCambioAsync(int id)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync("DELETE FROM tiposcambio WHERE IdTipoCambio = @Id", new { Id = id });
    }

    // ══════════════════════════════════════════════
    // OPCIONES DE PAGO
    // ══════════════════════════════════════════════

    public async Task<List<OpcionPago>> ObtenerOpcionesPagoAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<OpcionPago>(
            @"SELECT TOP 200 op.IdOpcionPago, op.Nombre AS OpcionPago1,
                     ISNULL(op.IdMoneda, 0) AS IdMoneda, m.Moneda AS NombreMoneda,
                     op.Logo, op.Activa, op.FechaCaptura, op.FechaUltEdicion,
                     op.IdUsuario
              FROM OpcionesPago op
              LEFT JOIN Monedas m ON m.IdMoneda = op.IdMoneda
              ORDER BY op.Nombre")).ToList();
    }

    public async Task CrearOpcionPagoAsync(string nombre, int? idMoneda, int? idLogo, bool activa, int idUsuario)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            @"INSERT INTO OpcionesPago (Nombre, IdMoneda, Logo, Activa, FechaCaptura, FechaUltEdicion, IdUsuario)
              VALUES (@Nombre, @IdMoneda, @Logo, @Activa, GETUTCDATE(), GETUTCDATE(), @IdUsuario)",
            new { Nombre = nombre, IdMoneda = idMoneda, Logo = idLogo?.ToString(), Activa = activa, IdUsuario = idUsuario });
    }

    public async Task ActualizarOpcionPagoAsync(int id, string nombre, int? idMoneda, int? idLogo, bool activa, int idUsuario)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            @"UPDATE OpcionesPago SET Nombre = @Nombre, IdMoneda = @IdMoneda, Logo = @Logo,
                     Activa = @Activa, FechaUltEdicion = GETUTCDATE(), IdUsuario = @IdUsuario
              WHERE IdOpcionPago = @Id",
            new { Id = id, Nombre = nombre, IdMoneda = idMoneda, Logo = idLogo?.ToString(), Activa = activa, IdUsuario = idUsuario });
    }

    public async Task EliminarOpcionPagoAsync(int id)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync("DELETE FROM OpcionesPago WHERE IdOpcionPago = @Id", new { Id = id });
    }
}
