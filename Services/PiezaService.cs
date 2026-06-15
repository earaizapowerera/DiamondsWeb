using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio para el CRUD de piezas sencillas de joyeria.
/// Migrado de frmSencillas.frm (VB6) ~4,616 lineas.
/// Formula de precio: Precio = CostoNeto * Utilidad * UtilidadExtra * Impuesto / Divisor * TCCotizacion
/// </summary>
public class PiezaService
{
    private readonly string _connectionString;
    private readonly ILogger<PiezaService> _logger;

    public PiezaService(string connectionString, ILogger<PiezaService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    // ==================== CATALOGOS ====================

    public async Task<List<ProveedorInfo>> ObtenerProveedoresAsync()
    {
        using var db = CreateConnection();
        var sql = @"SELECT TOP 50 Proveedor, NombreProveedor, DefaultUtilidad,
                     IdDefaultUtilidadExtra, IdMoneda, UtilidadExtra,
                     CaracteristicaDefault, CostoDefault, IdDivisor, IdTabla, UtilizarMoneda
                     FROM Proveedores ORDER BY NombreProveedor";
        return (await db.QueryAsync<ProveedorInfo>(sql)).ToList();
    }

    public async Task<List<ProveedorInfo>> BuscarProveedoresAsync(string texto)
    {
        using var db = CreateConnection();
        var sql = @"SELECT TOP 30 Proveedor, NombreProveedor, DefaultUtilidad,
                     IdDefaultUtilidadExtra, IdMoneda, UtilidadExtra,
                     CaracteristicaDefault, CostoDefault, IdDivisor, IdTabla, UtilizarMoneda
                     FROM Proveedores WHERE NombreProveedor LIKE @Texto
                     ORDER BY NombreProveedor";
        return (await db.QueryAsync<ProveedorInfo>(sql, new { Texto = $"%{texto}%" })).ToList();
    }

    public async Task<ProveedorInfo?> ObtenerProveedorAsync(int proveedor)
    {
        using var db = CreateConnection();
        var sql = @"SELECT TOP 1 Proveedor, NombreProveedor, DefaultUtilidad,
                     IdDefaultUtilidadExtra, IdMoneda, UtilidadExtra,
                     CaracteristicaDefault, CostoDefault, IdDivisor, IdTabla, UtilizarMoneda
                     FROM Proveedores WHERE Proveedor = @Proveedor";
        return await db.QueryFirstOrDefaultAsync<ProveedorInfo>(sql, new { Proveedor = proveedor });
    }

    public async Task<List<GrupoPieza>> ObtenerGruposAsync()
    {
        using var db = CreateConnection();
        return (await db.QueryAsync<GrupoPieza>(
            "SELECT TOP 50 IdGrupo, Grupo FROM Grupos ORDER BY Grupo")).ToList();
    }

    public async Task<List<Moneda>> ObtenerMonedasAsync()
    {
        using var db = CreateConnection();
        return (await db.QueryAsync<Moneda>(
            "SELECT TOP 20 IdMoneda, Moneda AS NombreMoneda, Extranjera FROM Monedas")).ToList();
    }

    public async Task<List<DivisorVenta>> ObtenerDivisoresAsync()
    {
        using var db = CreateConnection();
        return (await db.QueryAsync<DivisorVenta>(
            "SELECT TOP 30 IdDivisor, Divisor, Descripcion FROM Divisores ORDER BY Descripcion")).ToList();
    }

    public async Task<List<EtiquetaPlantilla>> ObtenerEtiquetasAsync()
    {
        using var db = CreateConnection();
        return (await db.QueryAsync<EtiquetaPlantilla>(
            "SELECT TOP 20 IdTabla, Descripcion FROM TablasJerarquias ORDER BY Descripcion")).ToList();
    }

    public async Task<TipoCambio?> ObtenerTipoCambioAsync(int idMoneda)
    {
        using var db = CreateConnection();
        var sql = @"SELECT TOP 1 IdTipoCambio, idMoneda AS IdMoneda,
                     TipoCambioCotizacion, TipoCambioVenta
                     FROM TiposCambio WHERE idMoneda = @IdMoneda
                     ORDER BY FechaCaptura DESC";
        return await db.QueryFirstOrDefaultAsync<TipoCambio>(sql, new { IdMoneda = idMoneda });
    }

    public async Task<List<RazonSocialProveedorCombo>> ObtenerRazonesSocialesAsync(int proveedor)
    {
        using var db = CreateConnection();
        var sql = @"SELECT TOP 20 rs.IdRazonSocialProveedor, rs.RazonSocialProveedor AS RazonSocial
                     FROM Razones_Sociales_Proveedores rs
                     INNER JOIN Razones_Sociales_Proveedores_Proveedores rsp
                       ON rs.IdRazonSocialProveedor = rsp.IdRazonSocialProveedor
                     WHERE rsp.Proveedor = @Proveedor
                     ORDER BY rs.RazonSocialProveedor";
        return (await db.QueryAsync<RazonSocialProveedorCombo>(sql, new { Proveedor = proveedor })).ToList();
    }

    public async Task<List<UtilidadExtraRango>> ObtenerRangosUtilidadExtraAsync()
    {
        using var db = CreateConnection();
        return (await db.QueryAsync<UtilidadExtraRango>(
            "SELECT TOP 50 Id, PrecioGramoDesde, PrecioGramoHasta, DefaultUtilidadExtra FROM utilidadextra_preciogramo ORDER BY PrecioGramoDesde")).ToList();
    }

    /// <summary>
    /// Calcula UtilidadExtra automatica basada en PrecioGramo * TCCotizacion
    /// cuando el proveedor tiene UtilidadExtra=-1
    /// </summary>
    public async Task<decimal> CalcularUtilidadExtraAsync(decimal precioGramo, decimal tcCotizacion)
    {
        var precioConvertido = precioGramo * tcCotizacion;
        using var db = CreateConnection();
        var sql = @"SELECT TOP 1 DefaultUtilidadExtra FROM utilidadextra_preciogramo
                     WHERE @Precio BETWEEN PrecioGramoDesde AND PrecioGramoHasta";
        var result = await db.QueryFirstOrDefaultAsync<decimal?>(sql, new { Precio = precioConvertido });
        return result ?? 1m;
    }

    // ==================== REMISIONES ====================

    public async Task<List<Remision>> ObtenerRemisionesAsync(int? proveedorId = null)
    {
        using var db = CreateConnection();
        var sql = @"SELECT TOP 50 r.IdRemision, r.Proveedor, p.NombreProveedor,
                     r.Remision AS NumeroRemision, r.FechaRemision, r.Consignacion,
                     r.IdUsuario, r.FechaCaptura, r.IdTienda, r.IdLocalizacion,
                     (SELECT TOP 1 COUNT(*) FROM Piezas WHERE IdRemision = r.IdRemision) AS CantidadPiezas,
                     ISNULL((SELECT TOP 1 SUM(CBTotal) FROM Piezas WHERE IdRemision = r.IdRemision), 0) AS TotalBruto,
                     ISNULL((SELECT TOP 1 SUM(CNTotal) FROM Piezas WHERE IdRemision = r.IdRemision), 0) AS TotalNeto
                     FROM Remisiones r
                     INNER JOIN Proveedores p ON r.Proveedor = p.Proveedor
                     WHERE (@ProveedorId IS NULL OR r.Proveedor = @ProveedorId)
                     ORDER BY r.IdRemision DESC";
        return (await db.QueryAsync<Remision>(sql, new { ProveedorId = proveedorId })).ToList();
    }

    public async Task<Remision?> ObtenerRemisionAsync(int idRemision)
    {
        using var db = CreateConnection();
        var sql = @"SELECT TOP 1 r.IdRemision, r.Proveedor, p.NombreProveedor,
                     r.Remision AS NumeroRemision, r.FechaRemision, r.Consignacion,
                     r.IdUsuario, r.FechaCaptura, r.IdTienda, r.IdLocalizacion
                     FROM Remisiones r
                     INNER JOIN Proveedores p ON r.Proveedor = p.Proveedor
                     WHERE r.IdRemision = @IdRemision";
        return await db.QueryFirstOrDefaultAsync<Remision>(sql, new { IdRemision = idRemision });
    }

    public async Task<int> CrearRemisionAsync(Remision remision)
    {
        using var db = CreateConnection();
        var sql = @"INSERT INTO Remisiones (Proveedor, Remision, FechaRemision, Consignacion, IdUsuario, FechaCaptura, FechaUltEdicion, IdTienda, IdLocalizacion)
                     VALUES (@Proveedor, @NumeroRemision, @FechaRemision, @Consignacion, @IdUsuario, GETUTCDATE(), GETUTCDATE(), @IdTienda, @IdLocalizacion);
                     SELECT CAST(SCOPE_IDENTITY() AS INT)";
        return await db.QuerySingleAsync<int>(sql, remision);
    }

    public async Task ActualizarRemisionAsync(Remision remision)
    {
        using var db = CreateConnection();
        var sql = @"UPDATE Remisiones SET Proveedor = @Proveedor, Remision = @NumeroRemision,
                     FechaRemision = @FechaRemision, Consignacion = @Consignacion,
                     FechaUltEdicion = GETUTCDATE()
                     WHERE IdRemision = @IdRemision";
        await db.ExecuteAsync(sql, remision);
    }

    public async Task<bool> EliminarRemisionAsync(int idRemision)
    {
        using var db = CreateConnection();
        // Verificar que no tenga piezas
        var count = await db.QuerySingleAsync<int>(
            "SELECT TOP 1 COUNT(*) FROM Piezas WHERE IdRemision = @Id", new { Id = idRemision });
        if (count > 0) return false;
        await db.ExecuteAsync("DELETE FROM Remisiones WHERE IdRemision = @Id", new { Id = idRemision });
        return true;
    }

    // ==================== FACTURAS ====================

    public async Task<Factura?> ObtenerFacturaAsync(int idFactura)
    {
        using var db = CreateConnection();
        var sql = @"SELECT TOP 1 f.IdFactura, f.FolioFactura, f.Proveedor,
                     p.NombreProveedor, f.IdRazonSocialProveedor,
                     f.FechaFactura, f.Pedimento, f.IdUsuario
                     FROM Facturas f
                     LEFT JOIN Proveedores p ON f.Proveedor = p.Proveedor
                     WHERE f.IdFactura = @IdFactura";
        return await db.QueryFirstOrDefaultAsync<Factura>(sql, new { IdFactura = idFactura });
    }

    public async Task<int> CrearFacturaAsync(Factura factura)
    {
        using var db = CreateConnection();
        var sql = @"INSERT INTO Facturas (FolioFactura, Proveedor, IdRazonSocialProveedor, FechaFactura, FechaCaptura, IdUsuario, IdTienda, IdLocalizacion, Pedimento)
                     VALUES (@FolioFactura, @Proveedor, @IdRazonSocialProveedor, @FechaFactura, GETUTCDATE(), @IdUsuario, NULL, NULL, @Pedimento);
                     SELECT CAST(SCOPE_IDENTITY() AS INT)";
        return await db.QuerySingleAsync<int>(sql, factura);
    }

    public async Task<List<Factura>> ObtenerFacturasAsync(string? filtro = null)
    {
        using var db = CreateConnection();
        var sql = @"SELECT TOP 50 f.IdFactura, f.FolioFactura, f.Proveedor,
                     p.NombreProveedor, f.IdRazonSocialProveedor,
                     f.FechaFactura, f.Pedimento, f.IdUsuario,
                     ISNULL((SELECT SUM(CBTotal) FROM Piezas WHERE IdFactura = f.IdFactura), 0) AS TotalBruto,
                     ISNULL((SELECT SUM(CNTotal) FROM Piezas WHERE IdFactura = f.IdFactura), 0) AS TotalNeto
                     FROM Facturas f
                     LEFT JOIN Proveedores p ON f.Proveedor = p.Proveedor
                     WHERE (@Filtro IS NULL
                        OR f.FolioFactura LIKE '%' + @Filtro + '%'
                        OR p.NombreProveedor LIKE '%' + @Filtro + '%'
                        OR CAST(f.IdFactura AS VARCHAR) LIKE '%' + @Filtro + '%')
                     ORDER BY f.IdFactura DESC";
        return (await db.QueryAsync<Factura>(sql, new { Filtro = filtro })).ToList();
    }

    // ==================== PIEZAS ====================

    public async Task<List<PiezaResumen>> ObtenerPiezasPorRemisionAsync(int idRemision)
    {
        using var db = CreateConnection();
        var sql = @"SELECT TOP 50 p.CodigoBarras, p.Descripcion, g.Grupo AS NombreGrupo,
                     p.CBTotal, p.CNTotal, p.Precio, p.Peso,
                     p.Kilates, p.Modelo, p.Linea, m.Moneda AS NombreMoneda, p.FechaCaptura
                     FROM Piezas p
                     LEFT JOIN Grupos g ON p.IdGrupo = g.IdGrupo
                     LEFT JOIN Monedas m ON p.IdMoneda = m.IdMoneda
                     WHERE p.IdRemision = @IdRemision
                     ORDER BY p.FechaCaptura DESC";
        return (await db.QueryAsync<PiezaResumen>(sql, new { IdRemision = idRemision })).ToList();
    }

    public async Task<RemisionTotales> ObtenerTotalesRemisionAsync(int idRemision)
    {
        using var db = CreateConnection();
        var sql = @"SELECT TOP 1
                     COUNT(*) AS Piezas,
                     ISNULL(SUM(Peso), 0) AS Peso,
                     ISNULL(SUM(CBTotal), 0) AS BrutoTotal,
                     ISNULL(SUM(CNTotal), 0) AS NetoTotal,
                     ISNULL(SUM(CBPieza + CBPeso), 0) AS BrutoNota,
                     ISNULL(SUM(CNPieza + CNPeso), 0) AS NetoNota
                     FROM Piezas WHERE IdRemision = @IdRemision";
        return await db.QuerySingleAsync<RemisionTotales>(sql, new { IdRemision = idRemision });
    }

    public async Task<Pieza?> ObtenerPiezaAsync(string codigoBarras)
    {
        using var db = CreateConnection();
        var sql = @"SELECT TOP 1 p.*, g.Grupo AS NombreGrupo, m.Moneda AS NombreMoneda,
                     prov.NombreProveedor, r.Remision AS NumeroRemision,
                     o.Observaciones
                     FROM Piezas p
                     LEFT JOIN Grupos g ON p.IdGrupo = g.IdGrupo
                     LEFT JOIN Monedas m ON p.IdMoneda = m.IdMoneda
                     LEFT JOIN Remisiones r ON p.IdRemision = r.IdRemision
                     LEFT JOIN Proveedores prov ON r.Proveedor = prov.Proveedor
                     LEFT JOIN Observaciones o ON p.CodigoBarras = o.CodigoBarras
                     WHERE p.CodigoBarras = @CodigoBarras";
        return await db.QueryFirstOrDefaultAsync<Pieza>(sql, new { CodigoBarras = codigoBarras });
    }

    /// <summary>
    /// Genera un nuevo codigo de barras incrementando el CONTADOR.
    /// Formato: {IdTienda}{secuencia con padding} = 6 digitos total.
    /// </summary>
    private async Task<string> GenerarCodigoBarrasAsync(IDbConnection db, IDbTransaction tx, int idTienda)
    {
        // Obtener y actualizar contador atomicamente
        var cb = await db.QuerySingleAsync<int>(
            "SELECT TOP 1 CodigoBarras + 1 FROM Contador", transaction: tx);
        await db.ExecuteAsync(
            "UPDATE Contador SET CodigoBarras = CodigoBarras + 1", transaction: tx);

        // Formato: IdTienda + secuencia con zero-padding a 6 digitos total
        var cbStr = cb.ToString();
        if (idTienda > 0)
        {
            var tiendaStr = idTienda.ToString();
            var padding = 6 - tiendaStr.Length - cbStr.Length;
            if (padding > 0)
                cbStr = tiendaStr + cbStr.PadLeft(cbStr.Length + padding, '0');
            else
                cbStr = tiendaStr + cbStr;
        }
        else
        {
            cbStr = cbStr.PadLeft(6, '0');
        }

        return cbStr;
    }

    public async Task<GuardarPiezaResult> CrearPiezaAsync(Pieza pieza, int? idEtiqueta)
    {
        try
        {
            using var db = CreateConnection();
            db.Open();
            using var tx = db.BeginTransaction();

            // Generar codigo de barras
            var codigoBarras = await GenerarCodigoBarrasAsync(db, tx, pieza.IdTienda ?? 1);
            pieza.CodigoBarras = codigoBarras;

            // Insertar etiqueta
            var sqlEtiqueta = @"INSERT INTO Etiquetas (CodigoBarras, IdLocalizacion, IdTienda, FechaCaptura, IdUsuario, FechaUltEdicion, Precio, IdTabla)
                                 VALUES (@CodigoBarras, @IdLocalizacion, @IdTienda, GETUTCDATE(), @IdUsuario, GETUTCDATE(), @Precio, @IdTabla)";
            await db.ExecuteAsync(sqlEtiqueta, new
            {
                pieza.CodigoBarras,
                IdLocalizacion = pieza.IdLocalizacion ?? 1,
                IdTienda = pieza.IdTienda ?? 1,
                pieza.IdUsuario,
                pieza.Precio,
                IdTabla = idEtiqueta ?? 2
            }, tx);

            // Insertar pieza
            var sqlPieza = @"INSERT INTO Piezas (CodigoBarras, Descripcion, IdRemision, IdFactura, IdGrupo,
                              CBPieza, DescPieza, CNPieza, Peso, PrecioGramo, CBPeso, DescPeso, CNPeso,
                              CBManoObra, DescManoObra, CNManoObra, DescripcionManoObra,
                              CBTotal, CNTotal, CBFactura, DescFactura, CNFactura,
                              IdMoneda, TCCotizacion, TCCosto, Utilidad, UtilidadExtra, Impuesto, Divisor, Precio,
                              Kilates, Modelo, Linea, Quilates, Color, Pureza, Corte, NumSerie, Obs1, Obs2,
                              FechaCaptura, IdUsuario, FechaUltEdicion, IdDivisor, IdTienda, IdLocalizacion,
                              ArchivoFoto, Faltante, IdStatus)
                              VALUES (@CodigoBarras, @Descripcion, @IdRemision, @IdFactura, @IdGrupo,
                              @CBPieza, @DescPieza, @CNPieza, @Peso, @PrecioGramo, @CBPeso, @DescPeso, @CNPeso,
                              @CBManoObra, @DescManoObra, @CNManoObra, @DescripcionManoObra,
                              @CBTotal, @CNTotal, @CBFactura, @DescFactura, @CNFactura,
                              @IdMoneda, @TCCotizacion, @TCCosto, @Utilidad, @UtilidadExtra, @Impuesto, @Divisor, @Precio,
                              @Kilates, @Modelo, @Linea, @Quilates, @Color, @Pureza, @Corte, @NumSerie, @Obs1, @Obs2,
                              GETUTCDATE(), @IdUsuario, GETUTCDATE(), @IdDivisor, @IdTienda, @IdLocalizacion,
                              @ArchivoFoto, 0, 1)";
            await db.ExecuteAsync(sqlPieza, pieza, tx);

            // Insertar observaciones si existen
            if (!string.IsNullOrWhiteSpace(pieza.Observaciones))
            {
                await db.ExecuteAsync(
                    "INSERT INTO Observaciones (CodigoBarras, Observaciones) VALUES (@CB, @Obs)",
                    new { CB = codigoBarras, Obs = pieza.Observaciones }, tx);
            }

            tx.Commit();
            _logger.LogInformation("Pieza creada: {CB} - {Desc}", codigoBarras, pieza.Descripcion);

            return new GuardarPiezaResult { Success = true, CodigoBarras = codigoBarras };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear pieza: {Desc}", pieza.Descripcion);
            return new GuardarPiezaResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<GuardarPiezaResult> ActualizarPiezaAsync(Pieza pieza)
    {
        try
        {
            using var db = CreateConnection();
            var sql = @"UPDATE Piezas SET
                         Descripcion = @Descripcion, IdGrupo = @IdGrupo,
                         CBPieza = @CBPieza, DescPieza = @DescPieza, CNPieza = @CNPieza,
                         Peso = @Peso, PrecioGramo = @PrecioGramo, CBPeso = @CBPeso, DescPeso = @DescPeso, CNPeso = @CNPeso,
                         CBManoObra = @CBManoObra, DescManoObra = @DescManoObra, CNManoObra = @CNManoObra, DescripcionManoObra = @DescripcionManoObra,
                         CBTotal = @CBTotal, CNTotal = @CNTotal,
                         CBFactura = @CBFactura, DescFactura = @DescFactura, CNFactura = @CNFactura,
                         IdMoneda = @IdMoneda, TCCotizacion = @TCCotizacion, TCCosto = @TCCosto,
                         Utilidad = @Utilidad, UtilidadExtra = @UtilidadExtra, Impuesto = @Impuesto, Divisor = @Divisor, Precio = @Precio,
                         Kilates = @Kilates, Modelo = @Modelo, Linea = @Linea,
                         Quilates = @Quilates, Color = @Color, Pureza = @Pureza, Corte = @Corte,
                         NumSerie = @NumSerie, Obs1 = @Obs1, Obs2 = @Obs2,
                         FechaUltEdicion = GETUTCDATE(), IdDivisor = @IdDivisor,
                         ArchivoFoto = @ArchivoFoto
                         WHERE CodigoBarras = @CodigoBarras";
            await db.ExecuteAsync(sql, pieza);

            // Actualizar observaciones
            var existeObs = await db.QueryFirstOrDefaultAsync<string>(
                "SELECT TOP 1 Observaciones FROM Observaciones WHERE CodigoBarras = @CB",
                new { CB = pieza.CodigoBarras });
            if (!string.IsNullOrWhiteSpace(pieza.Observaciones))
            {
                if (existeObs != null)
                    await db.ExecuteAsync("UPDATE Observaciones SET Observaciones = @Obs WHERE CodigoBarras = @CB",
                        new { CB = pieza.CodigoBarras, Obs = pieza.Observaciones });
                else
                    await db.ExecuteAsync("INSERT INTO Observaciones (CodigoBarras, Observaciones) VALUES (@CB, @Obs)",
                        new { CB = pieza.CodigoBarras, Obs = pieza.Observaciones });
            }

            // Actualizar etiqueta
            await db.ExecuteAsync(
                "UPDATE Etiquetas SET Precio = @Precio, FechaUltEdicion = GETUTCDATE() WHERE CodigoBarras = @CB",
                new { CB = pieza.CodigoBarras, pieza.Precio });

            _logger.LogInformation("Pieza actualizada: {CB}", pieza.CodigoBarras);
            return new GuardarPiezaResult { Success = true, CodigoBarras = pieza.CodigoBarras };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar pieza: {CB}", pieza.CodigoBarras);
            return new GuardarPiezaResult { Success = false, Error = ex.Message };
        }
    }

    // ==================== ELIMINACION CON PERMISOS ====================
    // Migrado de EliminarPieza() en frmSencillas.frm (VB6, lineas 3449-3509)
    // y frmPermisoCancelar.frm para autorizacion de supervisor.

    /// <summary>
    /// Verifica si el usuario tiene permiso para eliminar la pieza.
    /// Logica VB6:
    ///   1) Si etiqueta fue impresa (etiquetasimpresas) → requiere autorizacion
    ///   2) Si menos de 2 horas desde FechaCaptura → puede eliminar libre
    ///   3) Si 2+ horas → requiere autorizacion
    ///   4) Supervisores (PermisoUsuarios=1) siempre pueden
    ///   5) Pre-autorizacion en permisocancelar tambien permite
    /// </summary>
    public async Task<PermisoEliminarResult> VerificarPermisoEliminarAsync(string codigoBarras, int userId)
    {
        using var db = CreateConnection();

        var pieza = await db.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT TOP 1 CodigoBarras, Descripcion, FechaCaptura FROM Piezas WHERE CodigoBarras = @CB",
            new { CB = codigoBarras });

        if (pieza == null)
            return new PermisoEliminarResult
            {
                CodigoBarras = codigoBarras,
                MotivoRequerimiento = "La pieza no existe."
            };

        var result = new PermisoEliminarResult
        {
            CodigoBarras = (string)pieza.CodigoBarras,
            Descripcion = (string?)pieza.Descripcion,
            FechaCaptura = (DateTime?)pieza.FechaCaptura,
        };

        // Gate 1: etiqueta ya impresa
        var etiquetaCount = await db.QuerySingleAsync<int>(
            "SELECT TOP 1 COUNT(*) FROM etiquetasimpresas WHERE codigobarras = @CB",
            new { CB = codigoBarras });
        result.EtiquetaImpresa = etiquetaCount > 0;

        // Gate 2: ventana de 2 horas
        if (pieza.FechaCaptura != null)
        {
            var horas = (DateTime.UtcNow - (DateTime)pieza.FechaCaptura).TotalHours;
            result.DentroDeVentana = horas < 2;
        }

        // Gate 3: usuario supervisor
        var permisoUsuarios = await db.QueryFirstOrDefaultAsync<bool?>(
            "SELECT TOP 1 CAST(PermisoUsuarios AS BIT) FROM Usuarios WHERE IdUsuario = @Id",
            new { Id = userId });
        result.EsSupervisor = permisoUsuarios == true;

        // Gate 4: pre-autorizacion existente
        var preAuthCount = await db.QuerySingleAsync<int>(
            "SELECT TOP 1 COUNT(*) FROM permisocancelar WHERE codigobarras = @CB",
            new { CB = codigoBarras });
        result.PreAutorizado = preAuthCount > 0;

        // Determinar resultado
        if (!result.EtiquetaImpresa && result.DentroDeVentana)
        {
            // Dentro de ventana y sin etiqueta impresa → libre
            result.PuedeEliminar = true;
            result.RequiereAutorizacion = false;
        }
        else if (result.EsSupervisor || result.PreAutorizado)
        {
            // Supervisor o pre-autorizado → libre
            result.PuedeEliminar = true;
            result.RequiereAutorizacion = false;
        }
        else
        {
            // Requiere autorizacion de supervisor
            result.PuedeEliminar = false;
            result.RequiereAutorizacion = true;
            result.MotivoRequerimiento = result.EtiquetaImpresa
                ? "La etiqueta de esta pieza ya fue impresa. Se requiere autorizacion de supervisor."
                : "Han pasado mas de 2 horas desde la captura. Se requiere autorizacion de supervisor.";
        }

        return result;
    }

    /// <summary>
    /// Elimina una pieza con control de permisos completo.
    /// Si requiere autorizacion, valida credenciales de supervisor y registra en permisocancelar.
    /// Archiva en PiezasCanceladas, registra en bitacora, limpia replicacion.
    /// </summary>
    public async Task<EliminarPiezaResult> EliminarPiezaConPermisoAsync(
        string codigoBarras, int userId, int idTienda, string? motivo,
        string? supervisorNombre = null, string? supervisorPassword = null)
    {
        try
        {
            var permiso = await VerificarPermisoEliminarAsync(codigoBarras, userId);

            if (permiso.MotivoRequerimiento == "La pieza no existe.")
                return new EliminarPiezaResult { Success = false, Error = "La pieza no existe." };

            // Si requiere autorizacion, validar supervisor
            if (permiso.RequiereAutorizacion)
            {
                if (string.IsNullOrWhiteSpace(supervisorNombre) || string.IsNullOrWhiteSpace(supervisorPassword))
                    return new EliminarPiezaResult { Success = false, Error = "Se requiere autorizacion de supervisor." };

                using var dbAuth = CreateConnection();
                var supervisor = await dbAuth.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT TOP 1 IdUsuario, PermisoUsuarios FROM Usuarios WHERE Nombre = @Nombre AND Password = @Password",
                    new { Nombre = supervisorNombre, Password = supervisorPassword });

                if (supervisor == null)
                    return new EliminarPiezaResult { Success = false, Error = "Credenciales de supervisor invalidas o sin permisos." };

                bool tienePermiso = (bool)supervisor!.PermisoUsuarios;
                if (!tienePermiso)
                    return new EliminarPiezaResult { Success = false, Error = "El usuario no tiene permisos de supervisor." };

                // Registrar pre-autorizacion
                int supervisorId = (int)supervisor!.IdUsuario;
                await dbAuth.ExecuteAsync(
                    @"INSERT INTO permisocancelar (CodigoBarras, IdUsuarioAutorizador, FechaAutorizacion, Motivo)
                      VALUES (@CB, @SuperId, GETUTCDATE(), @Motivo)",
                    new { CB = codigoBarras, SuperId = supervisorId, Motivo = motivo });
            }

            // Ejecutar eliminacion
            using var db = CreateConnection();
            db.Open();
            using var tx = db.BeginTransaction();

            // 1. Archivar en PiezasCanceladas (SELECT * + FechaBorrado + IdUsuarioBorrado)
            await db.ExecuteAsync(
                @"INSERT INTO PiezasCanceladas
                  SELECT Piezas.*, GETUTCDATE() AS FechaBorrado, @UserId AS IdUsuarioBorrado
                  FROM Piezas WHERE CodigoBarras = @CB",
                new { CB = codigoBarras, UserId = userId }, tx);

            // 2. Eliminar de Piezas
            await db.ExecuteAsync(
                "DELETE FROM Piezas WHERE CodigoBarras = @CB",
                new { CB = codigoBarras }, tx);

            // 3. Si no quedan mas piezas con este codigo, eliminar etiqueta
            var remaining = await db.QuerySingleAsync<int>(
                "SELECT TOP 1 COUNT(*) FROM Piezas WHERE CodigoBarras = @CB",
                new { CB = codigoBarras }, tx);
            if (remaining == 0)
                await db.ExecuteAsync("DELETE FROM Etiquetas WHERE CodigoBarras = @CB",
                    new { CB = codigoBarras }, tx);

            // 4. Eliminar observaciones
            await db.ExecuteAsync(
                "DELETE FROM Observaciones WHERE CodigoBarras = @CB",
                new { CB = codigoBarras }, tx);

            // 5. Registrar en bitacora
            await db.ExecuteAsync(
                @"INSERT INTO bitacoraimpresionpiezas (CodigoBarras, FechaImpresion, idusuario)
                  VALUES (@CB, GETUTCDATE(), @UserId)",
                new { CB = codigoBarras, UserId = userId }, tx);

            // 6. Reset replicacion (ultimosmovimientos)
            await db.ExecuteAsync(
                @"DELETE FROM ultimosmovimientos WHERE idtienda = @IdTienda
                    AND (tabla = 'PiezasCanceladas' OR tabla = 'Piezas');
                  INSERT INTO ultimosmovimientos (idtienda, tabla) VALUES (@IdTienda, 'Piezas');
                  INSERT INTO ultimosmovimientos (idtienda, tabla) VALUES (@IdTienda, 'PiezasCanceladas')",
                new { IdTienda = idTienda }, tx);

            tx.Commit();
            _logger.LogInformation(
                "Pieza eliminada: {CB} por usuario {User}, motivo: {Motivo}",
                codigoBarras, userId, motivo ?? "(sin motivo)");

            return new EliminarPiezaResult
            {
                Success = true,
                Mensaje = $"Pieza {codigoBarras} eliminada correctamente."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar pieza: {CB}", codigoBarras);
            return new EliminarPiezaResult { Success = false, Error = ex.Message };
        }
    }

    // ==================== BUSQUEDA ====================

    public async Task<List<PiezaResumen>> BuscarPiezasAsync(string? texto, int? idRemision, int? idGrupo)
    {
        using var db = CreateConnection();
        var sql = @"SELECT TOP 50 p.CodigoBarras, p.Descripcion, g.Grupo AS NombreGrupo,
                     p.CBTotal, p.CNTotal, p.Precio, p.Peso,
                     p.Kilates, p.Modelo, p.Linea, m.Moneda AS NombreMoneda, p.FechaCaptura
                     FROM Piezas p
                     LEFT JOIN Grupos g ON p.IdGrupo = g.IdGrupo
                     LEFT JOIN Monedas m ON p.IdMoneda = m.IdMoneda
                     WHERE 1=1
                     AND (@Texto IS NULL OR p.Descripcion LIKE '%' + @Texto + '%' OR p.CodigoBarras LIKE '%' + @Texto + '%')
                     AND (@IdRemision IS NULL OR p.IdRemision = @IdRemision)
                     AND (@IdGrupo IS NULL OR p.IdGrupo = @IdGrupo)
                     ORDER BY p.FechaCaptura DESC";
        return (await db.QueryAsync<PiezaResumen>(sql, new { Texto = texto, IdRemision = idRemision, IdGrupo = idGrupo })).ToList();
    }

    public async Task<bool> TestConexionAsync()
    {
        try
        {
            using var db = CreateConnection();
            await db.QuerySingleAsync<int>("SELECT TOP 1 1");
            return true;
        }
        catch { return false; }
    }
}
