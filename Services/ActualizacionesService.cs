using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio para la pantalla "Actualización desde Facturas".
/// Vincula piezas a facturas de proveedor, CRUD de facturas,
/// asignación individual o por remisión completa.
/// Migrado de frmActualizaciones.frm (VB6).
/// </summary>
public class ActualizacionesService
{
    private readonly string _connectionString;
    private readonly ILogger<ActualizacionesService> _logger;

    public ActualizacionesService(string connectionString, ILogger<ActualizacionesService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    /// <summary>
    /// Registra movimiento en ultimosmovimientos para una tabla/tienda.
    /// Reemplaza los bloques hardcodeados con idtienda=1.
    /// </summary>
    private async Task RegistrarMovimientoAsync(IDbConnection db, string tabla, int idTienda)
    {
        await db.ExecuteAsync(@"
            IF EXISTS (SELECT TOP 1 1 FROM ultimosmovimientos WHERE tabla=@tabla AND idtienda=@idTienda)
                UPDATE ultimosmovimientos SET FechaMovimiento = GETUTCDATE()
                WHERE tabla=@tabla AND idtienda=@idTienda
            ELSE
                INSERT INTO ultimosmovimientos (idtienda, tabla, FechaMovimiento)
                VALUES (@idTienda, @tabla, GETUTCDATE())",
            new { tabla, idTienda });
    }

    // ───────────────────── Facturas CRUD ─────────────────────

    /// <summary>
    /// Busca facturas usando la vista vBuscaFacturas.
    /// Filtro opcional por folio, proveedor o razón social.
    /// </summary>
    public async Task<List<FacturaDto>> BuscarFacturasAsync(string? filtro)
    {
        using var db = CreateConnection();
        var sql = @"SELECT TOP 50
                        f.IdFactura, f.FolioFactura, f.Proveedor,
                        p.NombreProveedor,
                        f.IdRazonSocialProveedor,
                        rsp.RazonSocialProveedor,
                        f.FechaFactura, f.FechaCaptura, f.FechaUltEdicion,
                        f.IdUsuario, f.IdTienda, f.Pedimento
                     FROM facturas f
                     LEFT JOIN proveedores p ON p.Proveedor = f.Proveedor
                     LEFT JOIN razones_sociales_proveedores rsp
                        ON rsp.IdRazonSocialProveedor = f.IdRazonSocialProveedor
                     WHERE 1=1";

        if (!string.IsNullOrWhiteSpace(filtro))
        {
            sql += @" AND (f.FolioFactura LIKE @filtro
                       OR p.NombreProveedor LIKE @filtro
                       OR rsp.RazonSocialProveedor LIKE @filtro
                       OR CAST(f.IdFactura AS VARCHAR) = @filtroExacto)";
        }

        sql += " ORDER BY f.IdFactura DESC";

        return (await db.QueryAsync<FacturaDto>(sql, new
        {
            filtro = $"%{filtro}%",
            filtroExacto = filtro ?? ""
        })).ToList();
    }

    /// <summary>
    /// Obtiene una factura por ID con datos de proveedor y razón social.
    /// </summary>
    public async Task<FacturaDto?> ObtenerFacturaAsync(int idFactura)
    {
        using var db = CreateConnection();
        return await db.QueryFirstOrDefaultAsync<FacturaDto>(@"
            SELECT TOP 1
                f.IdFactura, f.FolioFactura, f.Proveedor,
                p.NombreProveedor,
                f.IdRazonSocialProveedor,
                rsp.RazonSocialProveedor,
                f.FechaFactura, f.FechaCaptura, f.FechaUltEdicion,
                f.IdUsuario, f.IdTienda, f.Pedimento
            FROM facturas f
            LEFT JOIN proveedores p ON p.Proveedor = f.Proveedor
            LEFT JOIN razones_sociales_proveedores rsp
                ON rsp.IdRazonSocialProveedor = f.IdRazonSocialProveedor
            WHERE f.IdFactura = @idFactura",
            new { idFactura });
    }

    /// <summary>
    /// Crea una factura nueva. Auto-genera IdFactura desde tabla contador.
    /// Replica la lógica VB6: select isnull(factura,0) from contador + update contador set factura=factura+1
    /// El IdFactura se forma como: IdTienda + contadorFactura (ej: tienda 1, contador 45 → 145)
    /// </summary>
    public async Task<int> CrearFacturaAsync(FacturaFormRequest req, int idUsuario, int idTienda)
    {
        using var db = CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();

        try
        {
            // Obtener siguiente ID y actualizar contador atómicamente
            var contadorActual = await db.ExecuteScalarAsync<int>(
                "SELECT ISNULL(Factura, 0) FROM contador", transaction: tx);

            await db.ExecuteAsync(
                "UPDATE contador SET Factura = Factura + 1", transaction: tx);

            // Formar IdFactura: idTienda concatenado con (contador+1)
            // VB6: txtIdFactura.Text = IdTienda + CStr(idfactura)
            var nuevoId = int.Parse($"{idTienda}{contadorActual + 1}");

            await db.ExecuteAsync(@"
                INSERT INTO facturas (IdFactura, FolioFactura, Proveedor,
                    IdRazonSocialProveedor, FechaFactura, FechaCaptura,
                    FechaUltEdicion, IdUsuario, IdTienda, Pedimento)
                VALUES (@IdFactura, @FolioFactura, @Proveedor,
                    @IdRazonSocialProveedor, @FechaFactura, GETUTCDATE(),
                    GETUTCDATE(), @IdUsuario, @IdTienda, @Pedimento)",
                new
                {
                    IdFactura = nuevoId,
                    req.FolioFactura,
                    req.Proveedor,
                    req.IdRazonSocialProveedor,
                    req.FechaFactura,
                    IdUsuario = idUsuario,
                    IdTienda = idTienda,
                    req.Pedimento
                }, tx);

            tx.Commit();
            _logger.LogInformation("Factura creada: IdFactura={Id}, Folio={Folio}",
                nuevoId, req.FolioFactura);
            return nuevoId;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Actualiza datos de una factura existente.
    /// </summary>
    public async Task ActualizarFacturaAsync(int idFactura, FacturaFormRequest req)
    {
        using var db = CreateConnection();
        await db.ExecuteAsync(@"
            UPDATE facturas SET
                FolioFactura = @FolioFactura,
                Proveedor = @Proveedor,
                IdRazonSocialProveedor = @IdRazonSocialProveedor,
                FechaFactura = @FechaFactura,
                FechaUltEdicion = GETUTCDATE(),
                Pedimento = @Pedimento
            WHERE IdFactura = @IdFactura",
            new
            {
                IdFactura = idFactura,
                req.FolioFactura,
                req.Proveedor,
                req.IdRazonSocialProveedor,
                req.FechaFactura,
                req.Pedimento
            });
    }

    /// <summary>
    /// Elimina una factura (solo si no tiene piezas vinculadas).
    /// </summary>
    public async Task<(bool ok, string mensaje)> EliminarFacturaAsync(int idFactura)
    {
        using var db = CreateConnection();
        // Verificar si tiene piezas
        var count = await db.ExecuteScalarAsync<int>(
            "SELECT TOP 1 COUNT(*) FROM piezas WHERE IdFactura = @id",
            new { id = idFactura });

        if (count > 0)
            return (false, $"No se puede eliminar: la factura tiene {count} pieza(s) vinculada(s). Quite las piezas primero.");

        var rows = await db.ExecuteAsync(
            "DELETE FROM facturas WHERE IdFactura = @id",
            new { id = idFactura });

        return rows > 0
            ? (true, "Factura eliminada correctamente.")
            : (false, "No se encontró la factura.");
    }

    // ───────────────────── Piezas disponibles ─────────────────────

    /// <summary>
    /// Obtiene piezas disponibles para asignar a una factura.
    /// Usa la vista vActualizaPiezas, excluye piezas ya asignadas a esta factura.
    /// Filtro opcional por código de barras, obs2, remisión o descripción.
    /// </summary>
    public async Task<List<PiezaDisponibleDto>> BuscarPiezasDisponiblesAsync(
        int idFactura, string? filtro)
    {
        using var db = CreateConnection();
        var sql = @"SELECT TOP 50
                        CodigoBarras, Obs2, IdFactura, IdRemision, Remision,
                        Proveedor, Descripcion, FechaCaptura,
                        TCCosto, CBPieza, CNPieza, DescPieza,
                        CostoMN, CostoBrutoMN, IdMoneda
                     FROM vActualizaPiezas
                     WHERE (IdFactura IS NULL OR IdFactura <> @idFactura)";

        if (!string.IsNullOrWhiteSpace(filtro))
        {
            sql += @" AND (CodigoBarras LIKE @filtro
                       OR Obs2 LIKE @filtro
                       OR CAST(IdRemision AS VARCHAR) LIKE @filtro
                       OR Descripcion LIKE @filtro)";
        }

        sql += " ORDER BY CodigoBarras";

        return (await db.QueryAsync<PiezaDisponibleDto>(sql, new
        {
            idFactura,
            filtro = $"%{filtro}%"
        })).ToList();
    }

    // ───────────────────── Piezas vinculadas a factura ─────────────────────

    /// <summary>
    /// Obtiene piezas ya vinculadas a una factura (grid derecho).
    /// </summary>
    public async Task<List<PiezaVinculadaDto>> ObtenerPiezasVinculadasAsync(int idFactura)
    {
        using var db = CreateConnection();
        return (await db.QueryAsync<PiezaVinculadaDto>(@"
            SELECT TOP 50
                CodigoBarras, Obs2, CBFactura, CNFactura,
                TCCosto, CBPieza, CNPieza
            FROM piezas
            WHERE IdFactura = @idFactura
            ORDER BY CodigoBarras",
            new { idFactura })).ToList();
    }

    /// <summary>
    /// Obtiene totales de factura (sum de costos de piezas vinculadas).
    /// </summary>
    public async Task<FacturaTotalesDto> ObtenerTotalesFacturaAsync(int idFactura)
    {
        using var db = CreateConnection();
        return await db.QueryFirstOrDefaultAsync<FacturaTotalesDto>(@"
            SELECT TOP 1
                ISNULL(SUM(CBFactura), 0) AS Bruto,
                ISNULL(SUM(CNFactura), 0) AS Neto,
                COUNT(*) AS CantidadPiezas
            FROM piezas
            WHERE IdFactura = @idFactura",
            new { idFactura }) ?? new FacturaTotalesDto();
    }

    // ───────────────────── Asignar / Quitar piezas ─────────────────────

    /// <summary>
    /// Asigna una pieza individual a una factura.
    /// Actualiza IdFactura, TCCosto, CBFactura, CNFactura, DescFactura en piezas y bajaspiezas.
    /// Replica la lógica VB6 de ActualizarFactura().
    /// </summary>
    public async Task<(bool ok, string mensaje)> AsignarPiezaAsync(AsignarPiezaRequest req, int idTienda = 1)
    {
        if (req.CBTotal <= 0 || req.CNTotal <= 0 || req.TCCosto <= 0)
            return (false, "Los costos (CB, CN, TC) deben ser mayores a cero.");

        using var db = CreateConnection();

        // Calcular CBFactura y CNFactura (costo * tipo cambio)
        var cbFactura = req.CBTotal * req.TCCosto;
        var cnFactura = req.CNTotal * req.TCCosto;
        var descFactura = cbFactura > 0
            ? 100m * (1m - (cnFactura / cbFactura))
            : 0m;

        // Validar que razón social del proveedor de la factura corresponda
        // al proveedor de la remisión de la pieza (warning, no bloqueante en web)

        await db.ExecuteAsync(@"
            UPDATE piezas SET
                IdFactura = @IdFactura,
                TCCosto = @TCCosto,
                CBFactura = @CBFactura,
                CNFactura = @CNFactura,
                DescFactura = @DescFactura,
                FechaUltEdicion = GETUTCDATE()
            WHERE CodigoBarras = @CodigoBarras",
            new
            {
                req.IdFactura,
                req.TCCosto,
                CBFactura = cbFactura,
                CNFactura = cnFactura,
                DescFactura = descFactura,
                req.CodigoBarras
            });

        // Actualizar también bajaspiezas (réplica del VB6)
        await db.ExecuteAsync(@"
            UPDATE bajaspiezas SET
                IdFactura = @IdFactura,
                TCCosto = @TCCosto,
                CBFactura = @CBFactura,
                CNFactura = @CNFactura,
                DescFactura = @DescFactura,
                FechaUltEdicion = GETUTCDATE()
            WHERE CodigoBarras = @CodigoBarras",
            new
            {
                req.IdFactura,
                req.TCCosto,
                CBFactura = cbFactura,
                CNFactura = cnFactura,
                DescFactura = descFactura,
                req.CodigoBarras
            });

        await RegistrarMovimientoAsync(db, "Piezas", idTienda);

        _logger.LogInformation("Pieza {CB} asignada a factura {IdFactura}",
            req.CodigoBarras, req.IdFactura);
        return (true, $"Pieza {req.CodigoBarras} asignada correctamente.");
    }

    /// <summary>
    /// Asigna todas las piezas de una remisión a una factura.
    /// Replica Command3_Click del VB6.
    /// </summary>
    public async Task<(bool ok, string mensaje, int piezasAfectadas)> AsignarRemisionCompletaAsync(
        int idFactura, int idRemision, decimal tipoCambio, int idTienda = 1)
    {
        if (tipoCambio <= 0)
            return (false, "El tipo de cambio debe ser mayor a cero.", 0);

        using var db = CreateConnection();

        var rows = await db.ExecuteAsync(@"
            UPDATE piezas SET
                IdFactura = @IdFactura,
                TCCosto = @TC,
                CBFactura = @TC * CBPieza,
                CNFactura = @TC * CNPieza,
                FechaUltEdicion = GETUTCDATE()
            WHERE IdRemision = @IdRemision
              AND (IdFactura IS NULL OR IdFactura <> @IdFactura)",
            new { IdFactura = idFactura, TC = tipoCambio, IdRemision = idRemision });

        await RegistrarMovimientoAsync(db, "Piezas", idTienda);

        _logger.LogInformation("Remisión {IdRem} ({Rows} piezas) asignada a factura {IdFact}",
            idRemision, rows, idFactura);
        return (true, $"{rows} pieza(s) de la remisión {idRemision} asignadas.", rows);
    }

    /// <summary>
    /// Quita una pieza de una factura (des-vincula).
    /// Solo si la pieza tiene remisión (VB6: si no tiene remisión, solo se puede reasignar).
    /// </summary>
    public async Task<(bool ok, string mensaje)> QuitarPiezaAsync(
        int idFactura, string codigoBarras, int idTienda = 1)
    {
        using var db = CreateConnection();

        // Verificar que la pieza tiene remisión
        var idRemision = await db.ExecuteScalarAsync<int?>(
            "SELECT TOP 1 IdRemision FROM piezas WHERE CodigoBarras = @cb",
            new { cb = codigoBarras });

        if (idRemision == null)
            return (false, "No se puede quitar: la pieza no tiene remisión. Solo se puede reasignar a otra factura.");

        await db.ExecuteAsync(@"
            UPDATE piezas SET
                IdFactura = NULL,
                FechaUltEdicion = GETUTCDATE()
            WHERE CodigoBarras = @cb AND IdFactura = @idFactura",
            new { cb = codigoBarras, idFactura });

        await RegistrarMovimientoAsync(db, "Piezas", idTienda);

        _logger.LogInformation("Pieza {CB} quitada de factura {IdFactura}",
            codigoBarras, idFactura);
        return (true, $"Pieza {codigoBarras} quitada de la factura.");
    }

    // ───────────────────── Combos / Catálogos ─────────────────────

    /// <summary>
    /// Obtiene proveedores para combo searchable.
    /// </summary>
    public async Task<List<ProveedorComboDto>> ObtenerProveedoresAsync()
    {
        using var db = CreateConnection();
        return (await db.QueryAsync<ProveedorComboDto>(
            "SELECT TOP 50 Proveedor, NombreProveedor FROM proveedores ORDER BY NombreProveedor"
        )).ToList();
    }

    /// <summary>
    /// Obtiene razones sociales vinculadas a un proveedor.
    /// </summary>
    public async Task<List<RazonSocialComboDto>> ObtenerRazonesSocialesAsync(int proveedor)
    {
        using var db = CreateConnection();
        return (await db.QueryAsync<RazonSocialComboDto>(@"
            SELECT TOP 50
                rsp.IdRazonSocialProveedor,
                rsp.RazonSocialProveedor
            FROM razones_sociales_proveedores rsp
            INNER JOIN razones_sociales_proveedores_proveedores rspp
                ON rsp.IdRazonSocialProveedor = rspp.IdRazonSocialProveedor
            WHERE rspp.Proveedor = @proveedor
            ORDER BY rsp.RazonSocialProveedor",
            new { proveedor })).ToList();
    }

    /// <summary>
    /// Obtiene el IdTienda por defecto (primer registro de Tiendas).
    /// </summary>
    public async Task<int> ObtenerIdTiendaAsync()
    {
        using var db = CreateConnection();
        return await db.ExecuteScalarAsync<int>("SELECT TOP 1 IdTienda FROM Tiendas") ;
    }

    /// <summary>
    /// Verifica conexión a la base de datos.
    /// </summary>
    public async Task<string> TestConexionAsync()
    {
        using var db = CreateConnection();
        var result = await db.ExecuteScalarAsync<int>("SELECT 1");
        return result == 1 ? "OK" : "Error";
    }
}
