using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio del Punto de Venta Web.
/// Migración de frmPuntodeVenta.frm (VB6) a .NET 9.
/// Flujo: sesión → escanear piezas → descuentos → pagos → cerrar nota (sp_DardeBaja) → imprimir.
/// </summary>
public class PuntoVentaService
{
    private readonly string _connectionString;
    private readonly ILogger<PuntoVentaService> _logger;

    // IdTienda fijo = 1 (tienda local, igual que el legacy)
    private const int IdTienda = 1;

    public PuntoVentaService(string connectionString, ILogger<PuntoVentaService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    // ═══════════════════════════════════════════════════════════════
    //  SESIONES
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Lista sesiones abiertas (notas sin cerrar en tabla Notas).
    /// Equivale a: ComboBusqueda de vNotas en el VB6.
    /// </summary>
    public async Task<List<NotaSesion>> ObtenerSesionesAbiertasAsync()
    {
        using var db = CreateConnection();
        var sql = @"SELECT TOP 50 n.IdNota, n.IdUsuario, u.Nombre AS NombreUsuario,
                           n.IdVendedor, n.NombreCliente, n.Telefonos, n.Comentarios,
                           n.Factura, n.FechaBaja, n.Bruto, n.Descuento, n.Neto,
                           n.Total, n.FormaPago
                    FROM Notas n
                    INNER JOIN Usuarios u ON u.IdUsuario = n.IdUsuario
                    ORDER BY n.IdNota DESC";
        return (await db.QueryAsync<NotaSesion>(sql)).ToList();
    }

    /// <summary>
    /// Obtiene una sesión por IdNota
    /// </summary>
    public async Task<NotaSesion?> ObtenerSesionAsync(int idNota)
    {
        using var db = CreateConnection();
        return await db.QueryFirstOrDefaultAsync<NotaSesion>(
            @"SELECT TOP 1 n.IdNota, n.IdUsuario, u.Nombre AS NombreUsuario,
                     n.IdVendedor, n.NombreCliente, n.Telefonos, n.Comentarios,
                     n.Factura, n.FechaBaja, n.Bruto, n.Descuento, n.Neto,
                     n.Total, n.FormaPago
              FROM Notas n
              INNER JOIN Usuarios u ON u.IdUsuario = n.IdUsuario
              WHERE n.IdNota = @IdNota",
            new { IdNota = idNota });
    }

    /// <summary>
    /// Crea nueva sesión de venta (inserta en Notas con counter).
    /// Equivale a: txtUsuario_LostFocus en el VB6.
    /// </summary>
    public async Task<NotaSesion> CrearSesionAsync(CrearSesionRequest req)
    {
        using var db = CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();
        try
        {
            // Verificar que el usuario existe
            var usuario = await db.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT TOP 1 IdUsuario, Nombre FROM Usuarios WHERE IdUsuario = @Id",
                new { Id = req.IdUsuario }, tx);
            if (usuario == null)
                throw new InvalidOperationException("El usuario no existe.");

            // Verificar que no tenga sesión abierta
            var sesionesAbiertas = await db.QueryFirstOrDefaultAsync<int>(
                "SELECT TOP 1 COUNT(*) FROM Notas WHERE IdUsuario = @Id",
                new { Id = req.IdUsuario }, tx);
            if (sesionesAbiertas > 0)
                throw new InvalidOperationException("No se puede abrir dos sesiones del mismo usuario al mismo tiempo.");

            // Obtener siguiente número de nota del contador
            var nota = await db.QueryFirstAsync<int>(
                "SELECT Nota FROM Contador", transaction: tx);
            await db.ExecuteAsync(
                "UPDATE Contador SET Nota = Nota + 1", transaction: tx);

            var idNota = IdTienda * 10000000 + nota; // formato: {IdTienda}{Nota}
            var fechaBaja = req.FechaBaja ?? DateTime.UtcNow;

            await db.ExecuteAsync(
                @"INSERT INTO Notas (IdNota, IdTienda, IdUsuario, IdVendedor, FechaBaja)
                  VALUES (@IdNota, @IdTienda, @IdUsuario, @IdVendedor, @FechaBaja)",
                new
                {
                    IdNota = idNota,
                    IdTienda,
                    IdUsuario = req.IdUsuario,
                    IdVendedor = req.IdUsuario, // vendedor = usuario por default
                    FechaBaja = fechaBaja
                }, tx);

            tx.Commit();

            _logger.LogInformation("Sesión POS creada: IdNota={IdNota}, Usuario={IdUsuario}", idNota, req.IdUsuario);

            return new NotaSesion
            {
                IdNota = idNota,
                IdUsuario = req.IdUsuario,
                NombreUsuario = (string)usuario.Nombre,
                IdVendedor = req.IdUsuario,
                FechaBaja = fechaBaja
            };
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Cancela sesión completa: borra piezas temporales, pagos y nota.
    /// Equivale a: Command5_Click en el VB6.
    /// </summary>
    public async Task CancelarSesionAsync(int idNota)
    {
        using var db = CreateConnection();
        await db.ExecuteAsync(
            @"DELETE PiezasNotasTemporal WHERE IdNota = @Id;
              DELETE PagosNotas WHERE IdNota = @Id;
              DELETE Notas WHERE IdNota = @Id;",
            new { Id = idNota });
        _logger.LogInformation("Sesión POS cancelada: IdNota={IdNota}", idNota);
    }

    // ═══════════════════════════════════════════════════════════════
    //  PIEZAS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Busca pieza por código de barras en Etiquetas→Piezas, CatalogoRepetidas, vCompuestas.
    /// Equivale a: AgregarPieza en el VB6.
    /// </summary>
    public async Task<PiezaLookupResult?> BuscarPiezaAsync(string codigoBarras)
    {
        using var db = CreateConnection();

        // 1. Buscar en Etiquetas + Piezas (pieza sencilla o componente)
        var sencilla = await db.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT TOP 1 ETIQUETAS.CodigoBarras,
                     ISNULL(Corte,'') AS Corte,
                     ISNULL(Etiquetas.Descripcion, Piezas.Descripcion) AS Descripcion,
                     ISNULL(Modelo,'') AS Modelo, ISNULL(Linea,'') AS Linea,
                     ISNULL(Kilates,'') AS Kilates, ISNULL(Quilates,0) AS Quilates,
                     ISNULL(Color,'') AS Color, ISNULL(Pureza,'') AS Pureza,
                     ISNULL(NumSerie,'') AS NumSerie,
                     ISNULL(Etiquetas.Precio, Piezas.Precio) AS Precio,
                     Divisores.Divisor,
                     ISNULL(Piezas.CBPadre,'') AS CBPadre
              FROM Divisores
              INNER JOIN Piezas ON Piezas.IdDivisor = Divisores.IdDivisor
              INNER JOIN Etiquetas ON Piezas.CodigoBarras = Etiquetas.CodigoBarras
              WHERE ETIQUETAS.CodigoBarras = @CB OR Piezas.Obs2 = @CB",
            new { CB = codigoBarras });

        if (sencilla != null)
        {
            string cbPadre = (string)sencilla.CBPadre;
            if (!string.IsNullOrEmpty(cbPadre))
            {
                // Es componente de pieza compuesta
                return new PiezaLookupResult
                {
                    CodigoBarras = sencilla.CodigoBarras,
                    Descripcion = sencilla.Descripcion,
                    Precio = (decimal)sencilla.Precio,
                    Divisor = (decimal)sencilla.Divisor,
                    CBPadre = cbPadre,
                    TipoPieza = "Componente"
                };
            }

            return new PiezaLookupResult
            {
                CodigoBarras = sencilla.CodigoBarras,
                Descripcion = (string)sencilla.Descripcion,
                Precio = (decimal)sencilla.Precio,
                Divisor = (decimal)sencilla.Divisor,
                TipoPieza = "Sencilla",
                Kilates = sencilla.Kilates,
                Modelo = sencilla.Modelo,
                Linea = sencilla.Linea,
                Quilates = (decimal)sencilla.Quilates,
                Color = sencilla.Color,
                Pureza = sencilla.Pureza,
                Corte = sencilla.Corte,
                NumSerie = sencilla.NumSerie
            };
        }

        // 2. Buscar en CatalogoRepetidas
        var repetida = await db.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT TOP 1 CR.CodigoBarras, CR.Descripcion, ISNULL(CR.Kilates,'') AS Kilates,
                     CR.Precio, D.Divisor
              FROM CatalogoRepetidas CR
              INNER JOIN Divisores D ON CR.IdDivisor = D.IdDivisor
              WHERE CR.CodigoBarras = @CB",
            new { CB = codigoBarras });

        if (repetida != null)
        {
            return new PiezaLookupResult
            {
                CodigoBarras = repetida.CodigoBarras,
                Descripcion = repetida.Descripcion,
                Precio = (decimal)repetida.Precio,
                Divisor = (decimal)repetida.Divisor,
                TipoPieza = "Repetida",
                Kilates = repetida.Kilates
            };
        }

        // 3. Buscar en vCompuestas
        var compuesta = await db.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT TOP 1 CodigoBarras, Descripcion, Precio
              FROM vCompuestas WHERE CodigoBarras = @CB",
            new { CB = codigoBarras });

        if (compuesta != null)
        {
            // Divisor de compuestas = divisor id=1
            var divisor = await db.QueryFirstAsync<decimal>(
                "SELECT TOP 1 Divisor FROM Divisores WHERE IdDivisor = 1");

            return new PiezaLookupResult
            {
                CodigoBarras = compuesta.CodigoBarras,
                Descripcion = compuesta.Descripcion,
                Precio = (decimal)compuesta.Precio,
                Divisor = divisor,
                TipoPieza = "Compuesta"
            };
        }

        return null;
    }

    /// <summary>
    /// Agrega pieza a la nota temporal (PiezasNotasTemporal).
    /// Calcula costo = Precio / Divisor (factura: / 1.15 adicional).
    /// </summary>
    public async Task<PiezaTemporal> AgregarPiezaAsync(AgregarPiezaRequest req)
    {
        var pieza = await BuscarPiezaAsync(req.CodigoBarras);
        if (pieza == null)
            throw new InvalidOperationException("No existe la pieza. Intente de nuevo.");

        if (pieza.TipoPieza == "Componente")
            throw new InvalidOperationException("Esta pieza es componente de una pieza compuesta. Contacte a un supervisor.");

        using var db = CreateConnection();

        // Verificar que no esté ya en una sesión (solo piezas sencillas)
        if (pieza.TipoPieza == "Sencilla")
        {
            var yaExiste = await db.QueryFirstOrDefaultAsync<int>(
                "SELECT TOP 1 COUNT(*) FROM PiezasNotasTemporal WHERE CodigoBarras = @CB",
                new { CB = pieza.CodigoBarras });
            if (yaExiste > 0)
                throw new InvalidOperationException("La pieza ya existe en alguna sesión abierta.");
        }

        // Calcular costo
        decimal costo;
        if (pieza.TipoPieza == "Compuesta")
            costo = pieza.Precio / pieza.Divisor;
        else if (req.EsFactura)
            costo = pieza.Precio / pieza.Divisor / 1.15m;
        else
            costo = pieza.Precio / pieza.Divisor;

        // Construir descripción detallada (igual que VB6)
        var desc = pieza.Descripcion;
        if (pieza.TipoPieza != "Compuesta")
        {
            if (!string.IsNullOrEmpty(pieza.Kilates))
            {
                desc += " " + pieza.Kilates;
                if (decimal.TryParse(pieza.Kilates, out _)) desc += "K";
            }
        }
        if (pieza.TipoPieza == "Sencilla")
        {
            if (!string.IsNullOrEmpty(pieza.Modelo)) desc += $" /Modelo={pieza.Modelo}";
            if (!string.IsNullOrEmpty(pieza.Linea)) desc += $" /Linea={pieza.Linea}";
            if (!string.IsNullOrEmpty(pieza.NumSerie)) desc += $" /NumSerie={pieza.NumSerie}";
            if (pieza.Quilates > 0) desc += $" / {pieza.Quilates} qt";
            if (!string.IsNullOrEmpty(pieza.Color)) desc += $" /Color={pieza.Color}";
            if (!string.IsNullOrEmpty(pieza.Pureza)) desc += $" /Pureza={pieza.Pureza}";
            if (!string.IsNullOrEmpty(pieza.Corte)) desc += $" /Corte={pieza.Corte}";
        }

        int cantidad = 1;
        if (pieza.TipoPieza == "Repetida")
        {
            cantidad = req.Cantidad ?? 1;
            if (cantidad < 1) cantidad = 1;
        }

        var total = costo * cantidad;

        await db.ExecuteAsync(
            @"INSERT INTO PiezasNotasTemporal (IdNota, CodigoBarras, Descripcion, SubTotal, Cantidad, Total)
              VALUES (@IdNota, @CB, @Desc, @SubTotal, @Cantidad, @Total)",
            new
            {
                req.IdNota,
                CB = pieza.CodigoBarras,
                Desc = desc,
                SubTotal = costo,
                Cantidad = cantidad,
                Total = total
            });

        _logger.LogInformation("Pieza agregada: {CB} ({Tipo}) a nota {IdNota}", pieza.CodigoBarras, pieza.TipoPieza, req.IdNota);

        return new PiezaTemporal
        {
            IdNota = req.IdNota,
            CodigoBarras = pieza.CodigoBarras,
            Descripcion = desc,
            Cantidad = (short)cantidad,
            SubTotal = costo,
            Total = total
        };
    }

    /// <summary>
    /// Lista piezas en la nota temporal
    /// </summary>
    public async Task<List<PiezaTemporal>> ObtenerPiezasTemporalesAsync(int idNota)
    {
        using var db = CreateConnection();
        return (await db.QueryAsync<PiezaTemporal>(
            @"SELECT TOP 50 IdNota, CodigoBarras, Descripcion, ISNULL(Cantidad,1) AS Cantidad,
                     SubTotal, Total
              FROM PiezasNotasTemporal WHERE IdNota = @Id",
            new { Id = idNota })).ToList();
    }

    /// <summary>
    /// Elimina pieza de la nota temporal
    /// </summary>
    public async Task EliminarPiezaAsync(int idNota, string codigoBarras)
    {
        using var db = CreateConnection();
        await db.ExecuteAsync(
            "DELETE PiezasNotasTemporal WHERE CodigoBarras = @CB AND IdNota = @IdNota",
            new { CB = codigoBarras, IdNota = idNota });
        _logger.LogInformation("Pieza eliminada: {CB} de nota {IdNota}", codigoBarras, idNota);
    }

    // ═══════════════════════════════════════════════════════════════
    //  PAGOS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Lista opciones de pago activas (catálogo OpcionesPago + Monedas)
    /// </summary>
    public async Task<List<OpcionPagoPOS>> ObtenerOpcionesPagoAsync()
    {
        using var db = CreateConnection();
        return (await db.QueryAsync<OpcionPagoPOS>(
            @"SELECT TOP 50 OP.IdOpcionPago, OP.OpcionPago, OP.IdMoneda,
                     M.Moneda AS NombreMoneda, M.Extranjera, OP.Logo
              FROM OpcionesPago OP
              INNER JOIN Monedas M ON M.IdMoneda = OP.IdMoneda
              WHERE OP.Activa <> 0
              ORDER BY OP.IdOpcionPago")).ToList();
    }

    /// <summary>
    /// Registra un pago en PagosNotas
    /// </summary>
    public async Task RegistrarPagoAsync(RegistrarPagoRequest req)
    {
        using var db = CreateConnection();
        await db.ExecuteAsync(
            @"INSERT INTO PagosNotas (IdNota, IdOpcionPago, Importe, TipoCambio, ImporteOriginal, FechaCaptura)
              VALUES (@IdNota, @IdOpcionPago, @Importe, @TipoCambio, @ImporteOriginal, GETUTCDATE())",
            new
            {
                req.IdNota,
                req.IdOpcionPago,
                req.Importe,
                req.TipoCambio,
                req.ImporteOriginal
            });
        _logger.LogInformation("Pago registrado: Nota={IdNota}, Opcion={Op}, Importe={Imp}",
            req.IdNota, req.IdOpcionPago, req.Importe);
    }

    /// <summary>
    /// Lista pagos de una nota (vista vPagosNotas)
    /// </summary>
    public async Task<List<PagoNotaDetalle>> ObtenerPagosAsync(int idNota)
    {
        using var db = CreateConnection();
        return (await db.QueryAsync<PagoNotaDetalle>(
            @"SELECT TOP 50 @IdNota AS IdNota, IdOpcionPago, OpcionPago, Importe, TipoCambio, ImporteOriginal
              FROM vPagosNotas WHERE IdNota = @IdNota",
            new { IdNota = idNota })).ToList();
    }

    /// <summary>
    /// Elimina un pago específico
    /// </summary>
    public async Task EliminarPagoAsync(int idNota, int idOpcionPago, decimal importe)
    {
        using var db = CreateConnection();
        // Eliminar solo una fila (puede haber pagos duplicados del mismo método)
        await db.ExecuteAsync(
            @"DELETE TOP(1) FROM PagosNotas
              WHERE IdNota = @IdNota AND IdOpcionPago = @IdOp AND Importe = @Imp",
            new { IdNota = idNota, IdOp = idOpcionPago, Imp = importe });
        _logger.LogInformation("Pago eliminado: Nota={IdNota}, Opcion={Op}", idNota, idOpcionPago);
    }

    // ═══════════════════════════════════════════════════════════════
    //  CÁLCULOS / RESUMEN
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Calcula el resumen de la nota (subtotal piezas, pagos, cambio).
    /// Equivale a: SumarPiezas + SumarPagos del VB6.
    /// </summary>
    public async Task<ResumenNota> CalcularResumenAsync(int idNota, decimal descuentoPct = 0, decimal sobrePrecio = 0, bool esFactura = false)
    {
        using var db = CreateConnection();

        // Suma de piezas
        var sumaPiezas = await db.QueryFirstAsync<decimal>(
            "SELECT ISNULL(SUM(Total), 0) FROM PiezasNotasTemporal WHERE IdNota = @Id",
            new { Id = idNota });

        // Suma de pagos
        var sumaPagos = await db.QueryFirstAsync<decimal>(
            "SELECT ISNULL(SUM(Importe), 0) FROM PagosNotas WHERE IdNota = @Id",
            new { Id = idNota });

        // Formas de pago (concatenadas)
        var formasPago = await db.QueryAsync<string>(
            @"SELECT OpcionPago FROM vPagosNotas
              WHERE Importe > 0 AND IdNota = @Id GROUP BY OpcionPago",
            new { Id = idNota });
        var formasPagoStr = string.Join("/", formasPago);

        decimal subTotal, total, totalFactura;

        if (!esFactura)
        {
            subTotal = sumaPiezas;
            if (descuentoPct > 0 || sobrePrecio != 0)
                total = subTotal * (1 - descuentoPct / 100) + sobrePrecio;
            else
                total = subTotal;
            totalFactura = 0;
        }
        else
        {
            // Factura: +15% IVA
            subTotal = sumaPiezas * 1.15m;
            total = subTotal * (1 - descuentoPct / 100);
            totalFactura = sumaPiezas * 1.15m;
        }

        var cambio = esFactura
            ? sumaPagos - totalFactura
            : sumaPagos - total;

        return new ResumenNota
        {
            SubTotal = Math.Round(subTotal, 2),
            Descuento = descuentoPct,
            SobrePrecio = sobrePrecio,
            Total = Math.Round(total, 2),
            TotalFactura = Math.Round(totalFactura, 2),
            TotalPagado = Math.Round(sumaPagos, 2),
            Cambio = Math.Round(cambio, 2),
            FormasPago = formasPagoStr,
            EsFactura = esFactura
        };
    }

    // ═══════════════════════════════════════════════════════════════
    //  CERRAR NOTA
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Cierra la nota: guarda datos finales en Notas, ejecuta sp_DardeBaja.
    /// Equivale a: cmdCerrarNota_Click del VB6.
    /// </summary>
    public async Task<int> CerrarNotaAsync(CerrarNotaRequest req)
    {
        using var db = CreateConnection();
        db.Open();

        // Validaciones
        if (string.IsNullOrWhiteSpace(req.NombreCliente))
            throw new InvalidOperationException("Falta el nombre del cliente.");

        var countPiezas = await db.QueryFirstAsync<int>(
            "SELECT COUNT(*) FROM PiezasNotasTemporal WHERE IdNota = @Id",
            new { Id = req.IdNota });
        if (countPiezas == 0)
            throw new InvalidOperationException("No se ha capturado ninguna pieza.");

        // Actualizar datos finales en la nota
        await db.ExecuteAsync(
            @"UPDATE Notas SET
                NombreCliente = @NombreCliente,
                Telefonos = @Telefonos,
                Comentarios = @Comentarios,
                Factura = @Factura,
                FechaBaja = @FechaBaja,
                Descuento = @Descuento,
                Bruto = @Bruto,
                Neto = @Neto,
                Total = @Total,
                FormaPago = @FormaPago,
                IdVendedor = @IdVendedor,
                FechaUltEdicion = GETUTCDATE()
              WHERE IdNota = @IdNota",
            new
            {
                req.IdNota,
                req.NombreCliente,
                req.Telefonos,
                req.Comentarios,
                req.Factura,
                req.FechaBaja,
                Descuento = req.Descuento ?? 0m,
                Bruto = req.Bruto ?? 0m,
                Neto = req.Neto ?? 0m,
                Total = req.Total ?? 0m,
                req.FormaPago,
                req.IdVendedor
            });

        // Ejecutar sp_DardeBaja — mueve todo a tablas definitivas
        await db.ExecuteAsync("EXEC sp_DardeBaja @IdNota", new { req.IdNota });

        _logger.LogInformation("Nota cerrada: IdNota={IdNota}, Cliente={Cliente}",
            req.IdNota, req.NombreCliente);

        return req.IdNota;
    }

    // ═══════════════════════════════════════════════════════════════
    //  NOTA CERRADA (para impresión)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Obtiene datos de una nota ya cerrada (BajasNotas) para impresión
    /// </summary>
    public async Task<NotaCerrada?> ObtenerNotaCerradaAsync(int idNota)
    {
        using var db = CreateConnection();

        var nota = await db.QueryFirstOrDefaultAsync<NotaCerrada>(
            @"SELECT TOP 1 bn.IdNota, bn.NombreCliente, bn.Telefonos, bn.Comentarios,
                     bn.Factura, bn.FechaBaja, bn.Bruto, bn.Descuento, bn.Neto,
                     bn.Total, bn.FormaPago, bn.IdUsuario, bn.IdVendedor,
                     u.Nombre AS NombreVendedor
              FROM BajasNotas bn
              LEFT JOIN Usuarios u ON u.IdUsuario = bn.IdVendedor
              WHERE bn.IdNota = @Id",
            new { Id = idNota });

        if (nota == null) return null;

        nota.Piezas = (await db.QueryAsync<PiezaNotaFinal>(
            @"SELECT TOP 50 IdPiezaNota, IdNota, CodigoBarras, Descripcion,
                     ISNULL(Cantidad,1) AS Cantidad, SubTotal, Total
              FROM PiezasNotas WHERE IdNota = @Id",
            new { Id = idNota })).ToList();

        nota.Pagos = (await db.QueryAsync<PagoNotaDetalle>(
            @"SELECT TOP 50 @IdNota AS IdNota, IdOpcionPago, OpcionPago, Importe, TipoCambio, ImporteOriginal
              FROM BajasPagosNotas bp
              INNER JOIN OpcionesPago op ON op.IdOpcionPago = bp.IdOpcionPago
              WHERE bp.IdNota = @IdNota",
            new { IdNota = idNota })).ToList();

        return nota;
    }

    // ═══════════════════════════════════════════════════════════════
    //  AUXILIARES
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Actualiza campos de la nota (nombre, teléfono, etc.) en tiempo real.
    /// Equivale a: txtNombre_LostFocus → Editar en el VB6.
    /// </summary>
    public async Task ActualizarNotaAsync(int idNota, string? nombreCliente = null,
        string? telefonos = null, string? comentarios = null, bool? factura = null,
        DateTime? fechaBaja = null, int? idVendedor = null)
    {
        using var db = CreateConnection();
        var sets = new List<string>();
        var parms = new DynamicParameters();
        parms.Add("IdNota", idNota);

        if (nombreCliente != null) { sets.Add("NombreCliente = @NombreCliente"); parms.Add("NombreCliente", nombreCliente); }
        if (telefonos != null) { sets.Add("Telefonos = @Telefonos"); parms.Add("Telefonos", telefonos); }
        if (comentarios != null) { sets.Add("Comentarios = @Comentarios"); parms.Add("Comentarios", comentarios); }
        if (factura.HasValue) { sets.Add("Factura = @Factura"); parms.Add("Factura", factura.Value); }
        if (fechaBaja.HasValue) { sets.Add("FechaBaja = @FechaBaja"); parms.Add("FechaBaja", fechaBaja.Value); }
        if (idVendedor.HasValue) { sets.Add("IdVendedor = @IdVendedor"); parms.Add("IdVendedor", idVendedor.Value); }

        if (sets.Count == 0) return;

        sets.Add("FechaUltEdicion = GETUTCDATE()");
        var sql = $"UPDATE Notas SET {string.Join(", ", sets)} WHERE IdNota = @IdNota";
        await db.ExecuteAsync(sql, parms);
    }

    /// <summary>
    /// Valida usuario Diamonds (tabla Usuarios)
    /// </summary>
    public async Task<(int IdUsuario, string Nombre)?> ValidarUsuarioAsync(int idUsuario)
    {
        using var db = CreateConnection();
        var u = await db.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT TOP 1 IdUsuario, Nombre FROM Usuarios WHERE IdUsuario = @Id",
            new { Id = idUsuario });
        return u == null ? null : ((int)u.IdUsuario, (string)u.Nombre);
    }

    /// <summary>
    /// Lista usuarios disponibles para selección
    /// </summary>
    public async Task<List<(int Id, string Nombre)>> ObtenerUsuariosAsync()
    {
        using var db = CreateConnection();
        var result = await db.QueryAsync<dynamic>(
            "SELECT TOP 50 IdUsuario, Nombre FROM Usuarios ORDER BY Nombre");
        return result.Select(u => ((int)u.IdUsuario, (string)u.Nombre)).ToList();
    }
}
