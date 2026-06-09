using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

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

    // ══════════════════════════════════════════════
    // GRUPOS
    // ══════════════════════════════════════════════
    public async Task<List<Grupo>> ObtenerGruposAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<Grupo>(
            "SELECT IdGrupo, Grupo AS Grupo1, IdUsuario, FechaCaptura FROM Grupos WHERE IdGrupo > 0 ORDER BY Grupo"
        )).ToList();
    }

    public async Task<Grupo?> ObtenerGrupoAsync(int id)
    {
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Grupo>(
            "SELECT IdGrupo, Grupo AS Grupo1, IdUsuario, FechaCaptura FROM Grupos WHERE IdGrupo = @Id", new { Id = id });
    }

    public async Task<int> CrearGrupoAsync(string nombre, int idUsuario)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO Grupos (Grupo, IdUsuario, FechaCaptura) OUTPUT INSERTED.IdGrupo VALUES (@Nombre, @IdUsuario, GETDATE())",
            new { Nombre = nombre, IdUsuario = idUsuario });
    }

    public async Task ActualizarGrupoAsync(int id, string nombre, int idUsuario)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE Grupos SET Grupo = @Nombre, IdUsuario = @IdUsuario WHERE IdGrupo = @Id",
            new { Id = id, Nombre = nombre, IdUsuario = idUsuario });
    }

    public async Task EliminarGrupoAsync(int id)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync("DELETE FROM Grupos WHERE IdGrupo = @Id", new { Id = id });
    }

    // ══════════════════════════════════════════════
    // MONEDAS
    // ══════════════════════════════════════════════
    public async Task<List<Moneda>> ObtenerMonedasAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<Moneda>(
            "SELECT IdMoneda, Moneda AS Moneda1, Extranjera, IdUsuario, FechaCaptura FROM Monedas WHERE IdMoneda > 0 ORDER BY Moneda"
        )).ToList();
    }

    public async Task<Moneda?> ObtenerMonedaAsync(int id)
    {
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Moneda>(
            "SELECT IdMoneda, Moneda AS Moneda1, Extranjera, IdUsuario, FechaCaptura FROM Monedas WHERE IdMoneda = @Id", new { Id = id });
    }

    public async Task<int> CrearMonedaAsync(string nombre, bool extranjera, int idUsuario)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO Monedas (Moneda, Extranjera, IdUsuario, FechaCaptura) OUTPUT INSERTED.IdMoneda VALUES (@Nombre, @Extranjera, @IdUsuario, GETDATE())",
            new { Nombre = nombre, Extranjera = extranjera, IdUsuario = idUsuario });
    }

    public async Task ActualizarMonedaAsync(int id, string nombre, bool extranjera, int idUsuario)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE Monedas SET Moneda = @Nombre, Extranjera = @Extranjera, IdUsuario = @IdUsuario WHERE IdMoneda = @Id",
            new { Id = id, Nombre = nombre, Extranjera = extranjera, IdUsuario = idUsuario });
    }

    public async Task EliminarMonedaAsync(int id)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync("DELETE FROM Monedas WHERE IdMoneda = @Id", new { Id = id });
    }

    // ══════════════════════════════════════════════
    // TIPOS DE CAMBIO
    // ══════════════════════════════════════════════
    public async Task<List<TipoCambio>> ObtenerTiposCambioAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<TipoCambio>(@"
            SELECT tc.IdTipoCambio, tc.IdMoneda, m.Moneda, tc.TipoCambioCotizacion, tc.TipoCambioVenta,
                   tc.IdUsuario, tc.FechaCaptura
            FROM TiposCambio tc
            INNER JOIN Monedas m ON tc.IdMoneda = m.IdMoneda
            WHERE tc.IdTipoCambio > 0
            ORDER BY tc.FechaCaptura DESC")).ToList();
    }

    public async Task<int> CrearTipoCambioAsync(int idMoneda, decimal cotizacion, decimal venta, int idUsuario)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO TiposCambio (IdMoneda, TipoCambioCotizacion, TipoCambioVenta, IdUsuario, FechaCaptura) OUTPUT INSERTED.IdTipoCambio VALUES (@IdMoneda, @Cotizacion, @Venta, @IdUsuario, GETDATE())",
            new { IdMoneda = idMoneda, Cotizacion = cotizacion, Venta = venta, IdUsuario = idUsuario });
    }

    public async Task EliminarTipoCambioAsync(int id)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync("DELETE FROM TiposCambio WHERE IdTipoCambio = @Id", new { Id = id });
    }

    // ══════════════════════════════════════════════
    // OPCIONES DE PAGO
    // ══════════════════════════════════════════════
    public async Task<List<OpcionPago>> ObtenerOpcionesPagoAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<OpcionPago>(@"
            SELECT op.IdOpcionPago, op.OpcionPago AS OpcionPago1, op.IdMoneda, m.Moneda,
                   op.IdLogo, op.Activo, op.IdUsuario, op.FechaCaptura
            FROM OpcionesPago op
            LEFT JOIN Monedas m ON op.IdMoneda = m.IdMoneda
            WHERE op.IdOpcionPago > 0
            ORDER BY op.OpcionPago")).ToList();
    }

    public async Task<int> CrearOpcionPagoAsync(string nombre, int? idMoneda, int? idLogo, bool activo, int idUsuario)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO OpcionesPago (OpcionPago, IdMoneda, IdLogo, Activo, IdUsuario, FechaCaptura) OUTPUT INSERTED.IdOpcionPago VALUES (@Nombre, @IdMoneda, @IdLogo, @Activo, @IdUsuario, GETDATE())",
            new { Nombre = nombre, IdMoneda = idMoneda, IdLogo = idLogo, Activo = activo, IdUsuario = idUsuario });
    }

    public async Task ActualizarOpcionPagoAsync(int id, string nombre, int? idMoneda, int? idLogo, bool activo, int idUsuario)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE OpcionesPago SET OpcionPago = @Nombre, IdMoneda = @IdMoneda, IdLogo = @IdLogo, Activo = @Activo, IdUsuario = @IdUsuario WHERE IdOpcionPago = @Id",
            new { Id = id, Nombre = nombre, IdMoneda = idMoneda, IdLogo = idLogo, Activo = activo, IdUsuario = idUsuario });
    }

    public async Task EliminarOpcionPagoAsync(int id)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync("DELETE FROM OpcionesPago WHERE IdOpcionPago = @Id", new { Id = id });
    }

    // ══════════════════════════════════════════════
    // DIVISORES (MULTIPLICADORES)
    // ══════════════════════════════════════════════
    public async Task<List<Divisor>> ObtenerDivisoresAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<Divisor>(
            "SELECT IdDivisor, Descripcion, Divisor AS ValorDivisor FROM Divisores ORDER BY Descripcion"
        )).ToList();
    }

    public async Task<int> CrearDivisorAsync(string descripcion, decimal valor)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO Divisores (Descripcion, Divisor) OUTPUT INSERTED.IdDivisor VALUES (@Descripcion, @Valor)",
            new { Descripcion = descripcion, Valor = valor });
    }

    public async Task ActualizarDivisorAsync(int id, string descripcion, decimal valor)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE Divisores SET Descripcion = @Descripcion, Divisor = @Valor WHERE IdDivisor = @Id",
            new { Id = id, Descripcion = descripcion, Valor = valor });
    }

    public async Task EliminarDivisorAsync(int id)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync("DELETE FROM Divisores WHERE IdDivisor = @Id", new { Id = id });
    }

    // ══════════════════════════════════════════════
    // PROVEEDORES
    // ══════════════════════════════════════════════
    public async Task<List<Proveedor>> ObtenerProveedoresAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<Proveedor>(@"
            SELECT Proveedor AS Proveedor1, NombreProveedor, Telefono, Direccion, Contacto,
                   IdDefaultCaracteristica, IdDefaultTipoCosto, IdDefaultUtilidad,
                   IdMoneda, MonedaDefault, UtilidadExtraPrecioGramo, IdUsuario, FechaCaptura
            FROM Proveedores WHERE Proveedor > 0 ORDER BY NombreProveedor")).ToList();
    }

    public async Task<Proveedor?> ObtenerProveedorAsync(int id)
    {
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Proveedor>(@"
            SELECT Proveedor AS Proveedor1, NombreProveedor, Telefono, Direccion, Contacto,
                   IdDefaultCaracteristica, IdDefaultTipoCosto, IdDefaultUtilidad,
                   IdMoneda, MonedaDefault, UtilidadExtraPrecioGramo, IdUsuario, FechaCaptura
            FROM Proveedores WHERE Proveedor = @Id", new { Id = id });
    }

    public async Task<int> CrearProveedorAsync(Proveedor p)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO Proveedores (NombreProveedor, Telefono, Direccion, Contacto,
                IdDefaultCaracteristica, IdDefaultTipoCosto, IdDefaultUtilidad,
                IdMoneda, MonedaDefault, UtilidadExtraPrecioGramo, IdUsuario, FechaCaptura)
            OUTPUT INSERTED.Proveedor
            VALUES (@NombreProveedor, @Telefono, @Direccion, @Contacto,
                @IdDefaultCaracteristica, @IdDefaultTipoCosto, @IdDefaultUtilidad,
                @IdMoneda, @MonedaDefault, @UtilidadExtraPrecioGramo, @IdUsuario, GETDATE())",
            new { p.NombreProveedor, p.Telefono, p.Direccion, p.Contacto,
                  p.IdDefaultCaracteristica, p.IdDefaultTipoCosto, p.IdDefaultUtilidad,
                  p.IdMoneda, p.MonedaDefault, p.UtilidadExtraPrecioGramo, p.IdUsuario });
    }

    public async Task ActualizarProveedorAsync(Proveedor p)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE Proveedores SET NombreProveedor = @NombreProveedor, Telefono = @Telefono,
                Direccion = @Direccion, Contacto = @Contacto,
                IdDefaultCaracteristica = @IdDefaultCaracteristica, IdDefaultTipoCosto = @IdDefaultTipoCosto,
                IdDefaultUtilidad = @IdDefaultUtilidad, IdMoneda = @IdMoneda,
                MonedaDefault = @MonedaDefault, UtilidadExtraPrecioGramo = @UtilidadExtraPrecioGramo,
                IdUsuario = @IdUsuario
            WHERE Proveedor = @Proveedor1", p);
    }

    public async Task EliminarProveedorAsync(int id)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync("DELETE FROM Proveedores WHERE Proveedor = @Id", new { Id = id });
    }

    // ══════════════════════════════════════════════
    // RAZONES SOCIALES PROVEEDORES
    // ══════════════════════════════════════════════
    public async Task<List<RazonSocialProveedor>> ObtenerRazonesSocialesAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<RazonSocialProveedor>(@"
            SELECT IdRazonSocialProveedor, RazonSocialProveedor AS RazonSocialProveedor1, RFC,
                   Calle, Colonia, CodigoPostal, Municipio, Estado, IdUsuario, FechaCaptura, FechaUltEdicion
            FROM Razones_Sociales_Proveedores
            WHERE IdRazonSocialProveedor > 0
            ORDER BY RazonSocialProveedor")).ToList();
    }

    public async Task<int> CrearRazonSocialAsync(RazonSocialProveedor rs)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO Razones_Sociales_Proveedores (RazonSocialProveedor, RFC, Calle, Colonia, CodigoPostal, Municipio, Estado, IdUsuario, FechaCaptura, FechaUltEdicion)
            OUTPUT INSERTED.IdRazonSocialProveedor
            VALUES (@RazonSocialProveedor1, @RFC, @Calle, @Colonia, @CodigoPostal, @Municipio, @Estado, @IdUsuario, GETDATE(), GETDATE())",
            new { rs.RazonSocialProveedor1, rs.RFC, rs.Calle, rs.Colonia, rs.CodigoPostal, rs.Municipio, rs.Estado, rs.IdUsuario });
    }

    public async Task ActualizarRazonSocialAsync(RazonSocialProveedor rs)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE Razones_Sociales_Proveedores SET RazonSocialProveedor = @RazonSocialProveedor1, RFC = @RFC,
                Calle = @Calle, Colonia = @Colonia, CodigoPostal = @CodigoPostal,
                Municipio = @Municipio, Estado = @Estado, IdUsuario = @IdUsuario, FechaUltEdicion = GETDATE()
            WHERE IdRazonSocialProveedor = @IdRazonSocialProveedor", rs);
    }

    public async Task EliminarRazonSocialAsync(int id)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync("DELETE FROM Razones_Sociales_Proveedores WHERE IdRazonSocialProveedor = @Id", new { Id = id });
    }

    public async Task<List<RazonSocialProveedorAsignacion>> ObtenerAsignacionesRazonSocialAsync(int idRazonSocial)
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<RazonSocialProveedorAsignacion>(@"
            SELECT rsp.IdRazonSocialProveedor, rsp.Proveedor, p.NombreProveedor,
                   rs.RazonSocialProveedor AS RazonSocial
            FROM Razones_Sociales_Proveedores_Proveedores rsp
            INNER JOIN Proveedores p ON rsp.Proveedor = p.Proveedor
            INNER JOIN Razones_Sociales_Proveedores rs ON rsp.IdRazonSocialProveedor = rs.IdRazonSocialProveedor
            WHERE rsp.IdRazonSocialProveedor = @Id", new { Id = idRazonSocial })).ToList();
    }

    public async Task AsignarRazonSocialProveedorAsync(int idRazonSocial, int proveedor)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            "INSERT INTO Razones_Sociales_Proveedores_Proveedores (IdRazonSocialProveedor, Proveedor) VALUES (@IdRS, @Prov)",
            new { IdRS = idRazonSocial, Prov = proveedor });
    }

    public async Task DesasignarRazonSocialProveedorAsync(int idRazonSocial, int proveedor)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM Razones_Sociales_Proveedores_Proveedores WHERE IdRazonSocialProveedor = @IdRS AND Proveedor = @Prov",
            new { IdRS = idRazonSocial, Prov = proveedor });
    }

    // ══════════════════════════════════════════════
    // CATÁLOGO REPETIDAS
    // ══════════════════════════════════════════════
    public async Task<List<CatalogoRepetida>> ObtenerCatalogoRepetidasAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<CatalogoRepetida>(@"
            SELECT cr.CodigoBarras, cr.Descripcion, cr.Proveedor, p.NombreProveedor,
                   cr.IdGrupo, g.Grupo, cr.Kilates, cr.Precio, cr.IdDivisor, cr.IdUsuario, cr.FechaCaptura
            FROM CatalogoRepetidas cr
            LEFT JOIN Proveedores p ON cr.Proveedor = p.Proveedor
            LEFT JOIN Grupos g ON cr.IdGrupo = g.IdGrupo
            ORDER BY cr.Descripcion")).ToList();
    }

    public async Task CrearCatalogoRepetidaAsync(CatalogoRepetida item)
    {
        // Auto-genera código de barras
        using var conn = CreateConnection();
        var cb = await conn.ExecuteScalarAsync<int>("SELECT ISNULL(codigobarras,0)+1 FROM contador");
        await conn.ExecuteAsync("UPDATE contador SET codigobarras = codigobarras + 1");
        item.CodigoBarras = cb.ToString("D6");

        await conn.ExecuteAsync(@"
            INSERT INTO CatalogoRepetidas (CodigoBarras, Descripcion, Proveedor, IdGrupo, Kilates, Precio, IdDivisor, IdUsuario, FechaCaptura)
            VALUES (@CodigoBarras, @Descripcion, @Proveedor, @IdGrupo, @Kilates, @Precio, @IdDivisor, @IdUsuario, GETDATE())", item);
    }

    public async Task ActualizarCatalogoRepetidaAsync(CatalogoRepetida item)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE CatalogoRepetidas SET Descripcion = @Descripcion, Proveedor = @Proveedor,
                IdGrupo = @IdGrupo, Kilates = @Kilates, Precio = @Precio, IdDivisor = @IdDivisor
            WHERE CodigoBarras = @CodigoBarras", item);
    }

    public async Task EliminarCatalogoRepetidaAsync(string codigoBarras)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync("DELETE FROM CatalogoRepetidas WHERE CodigoBarras = @CB", new { CB = codigoBarras });
    }

    // ══════════════════════════════════════════════
    // DEFAULTS FACTOR COMUNES
    // ══════════════════════════════════════════════
    public async Task<List<DefaultFactorComun>> ObtenerDefaultsFactorComunesAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<DefaultFactorComun>(@"
            SELECT IdDefault, DefaultImpuesto, DefaultDivisor, IdUsuario, FechaCaptura
            FROM DefaultsfactorComunes ORDER BY FechaCaptura DESC")).ToList();
    }

    public async Task<int> CrearDefaultFactorComunAsync(decimal impuesto, decimal divisor, int idUsuario)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO DefaultsfactorComunes (DefaultImpuesto, DefaultDivisor, IdUsuario, FechaCaptura) OUTPUT INSERTED.IdDefault VALUES (@Impuesto, @Divisor, @IdUsuario, GETDATE())",
            new { Impuesto = impuesto, Divisor = divisor, IdUsuario = idUsuario });
    }

    public async Task EliminarDefaultFactorComunAsync(int id)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync("DELETE FROM DefaultsfactorComunes WHERE IdDefault = @Id", new { Id = id });
    }

    // ══════════════════════════════════════════════
    // DEFAULTS UTILIDAD
    // ══════════════════════════════════════════════
    public async Task<List<DefaultUtilidad>> ObtenerDefaultsUtilidadAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<DefaultUtilidad>(@"
            SELECT IdDefaultUtilidad, DefaultUtilidad AS DefaultUtilidad1, DefaultUtilidadReloj, DefaultUtilidadGemas, IdUsuario, FechaCaptura
            FROM DefaultsUtilidad ORDER BY FechaCaptura DESC")).ToList();
    }

    public async Task<int> CrearDefaultUtilidadAsync(decimal utilidad, decimal? reloj, decimal? gemas, int idUsuario)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO DefaultsUtilidad (DefaultUtilidad, DefaultUtilidadReloj, DefaultUtilidadGemas, IdUsuario, FechaCaptura) OUTPUT INSERTED.IdDefaultUtilidad VALUES (@Utilidad, @Reloj, @Gemas, @IdUsuario, GETDATE())",
            new { Utilidad = utilidad, Reloj = reloj, Gemas = gemas, IdUsuario = idUsuario });
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
    // ══════════════════════════════════════════════
    public async Task<List<UtilidadExtraPrecioGramo>> ObtenerUtilidadExtraPrecioGramoAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<UtilidadExtraPrecioGramo>(@"
            SELECT IdUtilidadExtra, PrecioGramoDesde, PrecioGramoHasta, UtilidadExtra, IdUsuario, FechaCaptura
            FROM UtilidadExtra_PrecioGramo ORDER BY PrecioGramoDesde")).ToList();
    }

    public async Task<int> CrearUtilidadExtraPrecioGramoAsync(decimal desde, decimal hasta, decimal utilidad, int idUsuario)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO UtilidadExtra_PrecioGramo (PrecioGramoDesde, PrecioGramoHasta, UtilidadExtra, IdUsuario, FechaCaptura) OUTPUT INSERTED.IdUtilidadExtra VALUES (@Desde, @Hasta, @Utilidad, @IdUsuario, GETDATE())",
            new { Desde = desde, Hasta = hasta, Utilidad = utilidad, IdUsuario = idUsuario });
    }

    public async Task ActualizarUtilidadExtraPrecioGramoAsync(int id, decimal desde, decimal hasta, decimal utilidad)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE UtilidadExtra_PrecioGramo SET PrecioGramoDesde = @Desde, PrecioGramoHasta = @Hasta, UtilidadExtra = @Utilidad WHERE IdUtilidadExtra = @Id",
            new { Id = id, Desde = desde, Hasta = hasta, Utilidad = utilidad });
    }

    public async Task EliminarUtilidadExtraPrecioGramoAsync(int id)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync("DELETE FROM UtilidadExtra_PrecioGramo WHERE IdUtilidadExtra = @Id", new { Id = id });
    }

    // ══════════════════════════════════════════════
    // TABLAS DE JERARQUÍAS
    // ══════════════════════════════════════════════
    public async Task<List<TablaJerarquia>> ObtenerTablasJerarquiasAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<TablaJerarquia>(
            "SELECT IdTablaJerarquia, Descripcion, IdUsuario FROM TablasJerarquias ORDER BY Descripcion")).ToList();
    }

    public async Task<List<Jerarquia>> ObtenerJerarquiasAsync(int idTabla)
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<Jerarquia>(
            "SELECT IdJerarquia, IdTablaJerarquia, Columna, Orden FROM Jerarquias WHERE IdTablaJerarquia = @Id ORDER BY Orden",
            new { Id = idTabla })).ToList();
    }

    public async Task<int> CrearTablaJerarquiaAsync(string descripcion, int idUsuario)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO TablasJerarquias (Descripcion, IdUsuario) OUTPUT INSERTED.IdTablaJerarquia VALUES (@Desc, @IdUsuario)",
            new { Desc = descripcion, IdUsuario = idUsuario });
    }

    public async Task EliminarTablaJerarquiaAsync(int id)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync("DELETE FROM Jerarquias WHERE IdTablaJerarquia = @Id", new { Id = id });
        await conn.ExecuteAsync("DELETE FROM TablasJerarquias WHERE IdTablaJerarquia = @Id", new { Id = id });
    }

    public async Task<int> CrearJerarquiaAsync(int idTabla, string columna, int orden)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO Jerarquias (IdTablaJerarquia, Columna, Orden) OUTPUT INSERTED.IdJerarquia VALUES (@IdTabla, @Columna, @Orden)",
            new { IdTabla = idTabla, Columna = columna, Orden = orden });
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
}
