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
                     p.Kilates, p.Modelo, p.Linea, m.Moneda AS NombreMoneda, p.FechaCaptura,
                     p.Quilates, p.Color, p.Pureza, p.Corte, p.NumSerie,
                     pr.NombreProveedor
                     FROM Piezas p
                     LEFT JOIN Grupos g ON p.IdGrupo = g.IdGrupo
                     LEFT JOIN Monedas m ON p.IdMoneda = m.IdMoneda
                     LEFT JOIN Remisiones r ON p.IdRemision = r.IdRemision
                     LEFT JOIN vProveedores pr ON r.Proveedor = pr.Proveedor
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
            // Fallback to 1 is a safety net; callers (Alta.cshtml.cs) should set IdTienda from User claim
            var codigoBarras = await GenerarCodigoBarrasAsync(db, tx, pieza.IdTienda ?? 1);
            pieza.CodigoBarras = codigoBarras;

            // Insertar etiqueta
            // Fallbacks to 1 are safety nets; callers should set IdTienda/IdLocalizacion from User claim
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

    /// <summary>
    /// Valida si un usuario puede eliminar una pieza.
    /// Reglas VB6: ventana de 2 horas desde FechaCaptura, o usuario con permiso especial.
    /// </summary>
    public async Task<(bool permitido, string motivo)> ValidarPermisoEliminarAsync(
        string codigoBarras, int userId, int horasVentana = 2)
    {
        using var db = CreateConnection();
        var pieza = await db.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT TOP 1 FechaCaptura, IdUsuario FROM Piezas WHERE CodigoBarras = @CB",
            new { CB = codigoBarras });

        if (pieza == null)
            return (false, "Pieza no encontrada.");

        // Verificar si está en una nota de venta (no se puede eliminar si ya se vendió)
        var enNota = await db.ExecuteScalarAsync<int>(
            "SELECT TOP 1 COUNT(*) FROM PiezasNotasTemporal WHERE CodigoBarras = @CB",
            new { CB = codigoBarras });
        if (enNota > 0)
            return (false, "No se puede eliminar: la pieza está en una nota de venta activa.");

        var enBaja = await db.ExecuteScalarAsync<int>(
            "SELECT TOP 1 COUNT(*) FROM BajasPiezas WHERE CodigoBarras = @CB",
            new { CB = codigoBarras });
        if (enBaja > 0)
            return (false, "No se puede eliminar: la pieza ya fue dada de baja (vendida).");

        // Verificar ventana de tiempo (solo el creador tiene ventana de 2 horas)
        var fechaCaptura = (DateTime)pieza.FechaCaptura;
        var horasDesdeCreacion = (DateTime.UtcNow - fechaCaptura).TotalHours;
        var esCreador = (int)pieza.IdUsuario == userId;

        if (!esCreador)
            return (false, "Solo el usuario que capturó la pieza puede eliminarla.");

        if (horasDesdeCreacion > horasVentana)
            return (false, $"Fuera de la ventana de eliminación ({horasVentana} horas). La pieza fue capturada hace {horasDesdeCreacion:F1} horas.");

        return (true, "Permitido.");
    }

    /// <summary>
    /// Elimina una pieza con validación de permisos, copia a bitácora.
    /// Replica lógica VB6: ventana de 2 horas, solo el creador, no vendidas.
    /// </summary>
    public async Task<(bool ok, string mensaje)> EliminarPiezaConPermisosAsync(
        string codigoBarras, int userId, int horasVentana = 2)
    {
        var (permitido, motivo) = await ValidarPermisoEliminarAsync(codigoBarras, userId, horasVentana);
        if (!permitido)
            return (false, motivo);

        var ok = await EliminarPiezaAsync(codigoBarras, userId);
        return ok
            ? (true, $"Pieza {codigoBarras} eliminada correctamente.")
            : (false, "Error interno al eliminar la pieza.");
    }

    public async Task<bool> EliminarPiezaAsync(string codigoBarras, int userId)
    {
        try
        {
            using var db = CreateConnection();
            db.Open();
            using var tx = db.BeginTransaction();

            // Copiar a PiezasCanceladas para auditoría
            await db.ExecuteAsync(
                @"INSERT INTO PiezasCanceladas (CodigoBarras, Descripcion, IdRemision, IdFactura, IdGrupo,
                  CBPieza, DescPieza, CNPieza, Peso, PrecioGramo, CBPeso, DescPeso, CNPeso,
                  CBManoObra, DescManoObra, CNManoObra, CBTotal, CNTotal, Precio,
                  FechaCaptura, IdUsuario, FechaUltEdicion)
                  SELECT CodigoBarras, Descripcion, IdRemision, IdFactura, IdGrupo,
                  CBPieza, DescPieza, CNPieza, Peso, PrecioGramo, CBPeso, DescPeso, CNPeso,
                  CBManoObra, DescManoObra, CNManoObra, CBTotal, CNTotal, Precio,
                  FechaCaptura, @UserId, GETUTCDATE()
                  FROM Piezas WHERE CodigoBarras = @CB",
                new { CB = codigoBarras, UserId = userId }, tx);

            // Eliminar observaciones
            await db.ExecuteAsync(
                "DELETE FROM Observaciones WHERE CodigoBarras = @CB",
                new { CB = codigoBarras }, tx);

            // Eliminar etiqueta
            await db.ExecuteAsync(
                "DELETE FROM Etiquetas WHERE CodigoBarras = @CB",
                new { CB = codigoBarras }, tx);

            // Eliminar pieza
            await db.ExecuteAsync(
                "DELETE FROM Piezas WHERE CodigoBarras = @CB",
                new { CB = codigoBarras }, tx);

            tx.Commit();
            _logger.LogInformation("Pieza eliminada: {CB} por usuario {User}", codigoBarras, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar pieza: {CB}", codigoBarras);
            return false;
        }
    }

    // ==================== BUSQUEDA ====================

    public async Task<List<PiezaResumen>> BuscarPiezasAsync(string? texto, int? idRemision, int? idGrupo)
    {
        using var db = CreateConnection();
        var sql = @"SELECT TOP 50 p.CodigoBarras, p.Descripcion, g.Grupo AS NombreGrupo,
                     p.CBTotal, p.CNTotal, p.Precio, p.Peso,
                     p.Kilates, p.Modelo, p.Linea, m.Moneda AS NombreMoneda, p.FechaCaptura,
                     p.Quilates, p.Color, p.Pureza, p.Corte, p.NumSerie,
                     pr.NombreProveedor
                     FROM Piezas p
                     LEFT JOIN Grupos g ON p.IdGrupo = g.IdGrupo
                     LEFT JOIN Monedas m ON p.IdMoneda = m.IdMoneda
                     LEFT JOIN Remisiones r ON p.IdRemision = r.IdRemision
                     LEFT JOIN vProveedores pr ON r.Proveedor = pr.Proveedor
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
