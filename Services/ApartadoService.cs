using System.Data;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio del Punto de Venta Apartados.
/// Migración de frmPuntodeVentaApartados.frm (VB6) a .NET 9.
/// Flujo: sesión → escanear piezas → descuentos → pagos → cerrar nota (sp_DardeBaja).
/// Tablas: NotasApartado, PiezasNotasApartado, PagosNotasApartado.
/// </summary>
public class ApartadoService
{
    private readonly string _connectionString;
    private readonly ILogger<ApartadoService> _logger;
    private const int IdTienda = 1;

    public ApartadoService(string connectionString, ILogger<ApartadoService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    // ═══════════════════════════════════════════════════════════════
    //  SESIONES
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Lista sesiones abiertas de apartado (vNotasApartado).
    /// </summary>
    public async Task<List<ApartadoSesion>> ObtenerSesionesAbiertasAsync()
    {
        using var db = CreateConnection();
        var sql = @"SELECT TOP 50 n.IdNota, n.IdUsuario, u.Nombre AS NombreUsuario,
                           n.IdVendedor, n.NombreCliente, n.Telefonos, n.RFC,
                           n.Calle, n.CodigoPostal, n.Colonia, n.Ciudad, n.Estado,
                           n.Municipio, n.CodigoBarrasCliente, n.Factura,
                           n.Bruto, n.Descuento, n.Neto, n.Total, n.FormaPago
                    FROM NotasApartado n
                    INNER JOIN Usuarios u ON u.IdUsuario = n.IdUsuario
                    ORDER BY n.IdNota DESC";
        return (await db.QueryAsync<ApartadoSesion>(sql)).ToList();
    }

    /// <summary>
    /// Obtiene una sesión de apartado por IdNota
    /// </summary>
    public async Task<ApartadoSesion?> ObtenerSesionAsync(int idNota)
    {
        using var db = CreateConnection();
        return await db.QueryFirstOrDefaultAsync<ApartadoSesion>(
            @"SELECT TOP 1 n.IdNota, n.IdUsuario, u.Nombre AS NombreUsuario,
                     n.IdVendedor, n.NombreCliente, n.Telefonos, n.RFC,
                     n.Calle, n.CodigoPostal, n.Colonia, n.Ciudad, n.Estado,
                     n.Municipio, n.CodigoBarrasCliente, n.Factura,
                     n.Bruto, n.Descuento, n.Neto, n.Total, n.FormaPago
              FROM NotasApartado n
              INNER JOIN Usuarios u ON u.IdUsuario = n.IdUsuario
              WHERE n.IdNota = @IdNota",
            new { IdNota = idNota });
    }

    /// <summary>
    /// Crea nueva sesión de apartado.
    /// Equivale a: txtUsuario_LostFocus en frmPuntodeVentaApartados.frm.
    /// </summary>
    public async Task<ApartadoSesion> CrearSesionAsync(CrearApartadoSesionRequest req)
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

            // Verificar que no tenga sesión abierta de apartado
            var sesionesAbiertas = await db.QueryFirstOrDefaultAsync<int>(
                "SELECT TOP 1 COUNT(*) FROM NotasApartado WHERE IdUsuario = @Id",
                new { Id = req.IdUsuario }, tx);
            if (sesionesAbiertas > 0)
                throw new InvalidOperationException("No se puede abrir dos sesiones del mismo usuario al mismo tiempo. Cierre la sesión anterior.");

            // Obtener siguiente número de nota del contador
            var nota = await db.QueryFirstAsync<int>(
                "SELECT Nota FROM Contador", transaction: tx);
            await db.ExecuteAsync(
                "UPDATE Contador SET Nota = Nota + 1", transaction: tx);

            var idNota = IdTienda * 10000000 + nota;

            await db.ExecuteAsync(
                @"INSERT INTO NotasApartado (IdNota, IdTienda, IdUsuario, IdVendedor)
                  VALUES (@IdNota, @IdTienda, @IdUsuario, @IdVendedor)",
                new
                {
                    IdNota = idNota,
                    IdTienda,
                    IdUsuario = req.IdUsuario,
                    IdVendedor = req.IdUsuario
                }, tx);

            tx.Commit();
            _logger.LogInformation("Sesión Apartado creada: IdNota={IdNota}, Usuario={IdUsuario}", idNota, req.IdUsuario);

            return new ApartadoSesion
            {
                IdNota = idNota,
                IdUsuario = req.IdUsuario,
                NombreUsuario = (string)usuario.Nombre,
                IdVendedor = req.IdUsuario
            };
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Cancela sesión de apartado completa.
    /// Equivale a: Command5_Click en VB6.
    /// </summary>
    public async Task CancelarSesionAsync(int idNota)
    {
        using var db = CreateConnection();
        await db.ExecuteAsync(
            @"DELETE PiezasNotasApartado WHERE IdNota = @Id;
              DELETE PagosNotasApartado WHERE IdNota = @Id;
              DELETE NotasApartado WHERE IdNota = @Id;",
            new { Id = idNota });
        _logger.LogInformation("Sesión Apartado cancelada: IdNota={IdNota}", idNota);
    }

    // ═══════════════════════════════════════════════════════════════
    //  PIEZAS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Busca pieza por código de barras en Etiquetas→Piezas y CatalogoRepetidas.
    /// Misma lógica que PuntoVentaService.BuscarPiezaAsync.
    /// </summary>
    public async Task<PiezaLookupResult?> BuscarPiezaAsync(string codigoBarras)
    {
        using var db = CreateConnection();

        // 1. Buscar en Etiquetas + Piezas (pieza sencilla)
        var sencilla = await db.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT TOP 1 ETIQUETAS.CodigoBarras,
                     ISNULL(Corte,'') AS Corte,
                     ISNULL(Etiquetas.Descripcion, Piezas.Descripcion) AS Descripcion,
                     ISNULL(Modelo,'') AS Modelo, ISNULL(Linea,'') AS Linea,
                     ISNULL(Kilates,'') AS Kilates, ISNULL(Quilates,0) AS Quilates,
                     ISNULL(Color,'') AS Color, ISNULL(Pureza,'') AS Pureza,
                     ISNULL(NumSerie,'') AS NumSerie,
                     ISNULL(Obs2,'') AS Obs2,
                     ISNULL(Etiquetas.Precio, Piezas.Precio) AS Precio,
                     Divisores.Divisor
              FROM Divisores
              INNER JOIN Piezas ON Piezas.IdDivisor = Divisores.IdDivisor
              INNER JOIN Etiquetas ON Piezas.CodigoBarras = Etiquetas.CodigoBarras
              WHERE ETIQUETAS.CodigoBarras = @CB OR Piezas.Obs2 = @CB",
            new { CB = codigoBarras });

        if (sencilla != null)
        {
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

        return null;
    }

    /// <summary>
    /// Agrega pieza a la nota de apartado (PiezasNotasApartado).
    /// </summary>
    public async Task<PiezaApartado> AgregarPiezaAsync(AgregarPiezaApartadoRequest req)
    {
        var pieza = await BuscarPiezaAsync(req.CodigoBarras);
        if (pieza == null)
            throw new InvalidOperationException("No existe la pieza. Intente de nuevo.");

        using var db = CreateConnection();

        // Verificar duplicada en sesiones (solo sencillas)
        if (pieza.TipoPieza == "Sencilla")
        {
            var yaExiste = await db.QueryFirstOrDefaultAsync<int>(
                "SELECT TOP 1 COUNT(*) FROM PiezasNotasApartado WHERE CodigoBarras = @CB",
                new { CB = pieza.CodigoBarras });
            if (yaExiste > 0)
                throw new InvalidOperationException("La pieza ya existe en alguna sesión abierta.");
        }

        // Calcular costo
        decimal costo = req.EsFactura
            ? pieza.Precio / pieza.Divisor / 1.15m
            : pieza.Precio / pieza.Divisor;

        // Construir descripción detallada
        var desc = pieza.Descripcion;
        if (!string.IsNullOrEmpty(pieza.Kilates))
        {
            desc += " " + pieza.Kilates;
            if (decimal.TryParse(pieza.Kilates, out _)) desc += "K";
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

        int cantidad = pieza.TipoPieza == "Repetida" ? (req.Cantidad ?? 1) : 1;
        if (cantidad < 1) cantidad = 1;
        var total = costo * cantidad;

        await db.ExecuteAsync(
            @"INSERT INTO PiezasNotasApartado (IdNota, CodigoBarras, Descripcion, SubTotal, Cantidad, Total)
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

        _logger.LogInformation("Pieza apartado agregada: {CB} ({Tipo}) a nota {IdNota}", pieza.CodigoBarras, pieza.TipoPieza, req.IdNota);

        return new PiezaApartado
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
    /// Lista piezas de la nota de apartado
    /// </summary>
    public async Task<List<PiezaApartado>> ObtenerPiezasAsync(int idNota)
    {
        using var db = CreateConnection();
        return (await db.QueryAsync<PiezaApartado>(
            @"SELECT TOP 50 IdNota, CodigoBarras, Descripcion, ISNULL(Cantidad,1) AS Cantidad,
                     SubTotal, Total
              FROM PiezasNotasApartado WHERE IdNota = @Id",
            new { Id = idNota })).ToList();
    }

    /// <summary>
    /// Elimina pieza de la nota de apartado
    /// </summary>
    public async Task EliminarPiezaAsync(int idNota, string codigoBarras)
    {
        using var db = CreateConnection();
        await db.ExecuteAsync(
            "DELETE PiezasNotasApartado WHERE CodigoBarras = @CB AND IdNota = @IdNota",
            new { CB = codigoBarras, IdNota = idNota });
        _logger.LogInformation("Pieza apartado eliminada: {CB} de nota {IdNota}", codigoBarras, idNota);
    }

    // ═══════════════════════════════════════════════════════════════
    //  PAGOS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Lista opciones de pago activas (misma que POS regular)
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
    /// Registra pago en PagosNotasApartado
    /// </summary>
    public async Task RegistrarPagoAsync(RegistrarPagoApartadoRequest req)
    {
        using var db = CreateConnection();
        await db.ExecuteAsync(
            @"INSERT INTO PagosNotasApartado (IdNota, IdOpcionPago, Importe, TipoCambio, ImporteOriginal)
              VALUES (@IdNota, @IdOpcionPago, @Importe, @TipoCambio, @ImporteOriginal)",
            new
            {
                req.IdNota,
                req.IdOpcionPago,
                req.Importe,
                req.TipoCambio,
                req.ImporteOriginal
            });
        _logger.LogInformation("Pago apartado registrado: Nota={IdNota}, Opcion={Op}, Importe={Imp}",
            req.IdNota, req.IdOpcionPago, req.Importe);
    }

    /// <summary>
    /// Lista pagos de una nota de apartado
    /// </summary>
    public async Task<List<PagoApartadoDetalle>> ObtenerPagosAsync(int idNota)
    {
        using var db = CreateConnection();
        return (await db.QueryAsync<PagoApartadoDetalle>(
            @"SELECT TOP 50 @IdNota AS IdNota, IdOpcionPago, OpcionPago, Importe,
                     ISNULL(TipoCambio,0) AS TipoCambio, ISNULL(ImporteOriginal,0) AS ImporteOriginal
              FROM vPagosNotasApartado WHERE IdNota = @IdNota",
            new { IdNota = idNota })).ToList();
    }

    /// <summary>
    /// Elimina un pago específico de apartado
    /// </summary>
    public async Task EliminarPagoAsync(int idNota, int idOpcionPago, decimal importe)
    {
        using var db = CreateConnection();
        await db.ExecuteAsync(
            @"DELETE TOP(1) FROM PagosNotasApartado
              WHERE IdNota = @IdNota AND IdOpcionPago = @IdOp AND Importe = @Imp",
            new { IdNota = idNota, IdOp = idOpcionPago, Imp = importe });
        _logger.LogInformation("Pago apartado eliminado: Nota={IdNota}, Opcion={Op}", idNota, idOpcionPago);
    }

    // ═══════════════════════════════════════════════════════════════
    //  CÁLCULOS / RESUMEN
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Calcula resumen de totales de la nota de apartado.
    /// Equivale a: SumarPiezas + SumarPagos del VB6.
    /// </summary>
    public async Task<ResumenApartado> CalcularResumenAsync(int idNota, decimal descuentoPct = 0, decimal sobrePrecio = 0, bool esFactura = false)
    {
        using var db = CreateConnection();

        var sumaPiezas = await db.QueryFirstAsync<decimal>(
            "SELECT ISNULL(SUM(Total), 0) FROM PiezasNotasApartado WHERE IdNota = @Id",
            new { Id = idNota });

        var sumaPagos = await db.QueryFirstAsync<decimal>(
            "SELECT ISNULL(SUM(Importe), 0) FROM PagosNotasApartado WHERE IdNota = @Id",
            new { Id = idNota });

        var formasPago = await db.QueryAsync<string>(
            @"SELECT OpcionPago FROM vPagosNotasApartado
              WHERE Importe > 0 AND IdNota = @Id GROUP BY OpcionPago",
            new { Id = idNota });
        var formasPagoStr = string.Join("/", formasPago);

        decimal subTotal, total, totalFactura;

        if (!esFactura)
        {
            subTotal = sumaPiezas;
            total = (descuentoPct > 0 || sobrePrecio != 0)
                ? subTotal * (1 - descuentoPct / 100) + sobrePrecio
                : subTotal;
            totalFactura = 0;
        }
        else
        {
            subTotal = sumaPiezas * 1.15m;
            total = subTotal * (1 - descuentoPct / 100);
            totalFactura = sumaPiezas * 1.15m;
        }

        var cambio = esFactura ? sumaPagos - totalFactura : sumaPagos - total;

        return new ResumenApartado
        {
            SubTotal = subTotal,
            DescuentoPct = descuentoPct,
            SobrePrecio = sobrePrecio,
            Total = total,
            TotalFactura = totalFactura,
            TotalPagado = sumaPagos,
            Cambio = cambio,
            FormasPago = formasPagoStr,
            EsFactura = esFactura
        };
    }

    // ═══════════════════════════════════════════════════════════════
    //  ACTUALIZAR NOTA / CERRAR
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Actualiza datos del cliente y nota de apartado.
    /// Equivale a: Editar(Me, "Notasapartado", ...) en VB6.
    /// </summary>
    public async Task ActualizarNotaAsync(ActualizarApartadoNotaReq req)
    {
        using var db = CreateConnection();
        await db.ExecuteAsync(
            @"UPDATE NotasApartado SET
                NombreCliente = @NombreCliente, Telefonos = @Telefonos,
                RFC = @RFC, Calle = @Calle, CodigoPostal = @CodigoPostal,
                Colonia = @Colonia, Ciudad = @Ciudad, Estado = @Estado,
                Municipio = @Municipio, CodigoBarrasCliente = @CodigoBarrasCliente,
                Factura = @Factura, IdVendedor = @IdVendedor
              WHERE IdNota = @IdNota",
            new
            {
                req.IdNota,
                req.NombreCliente,
                req.Telefonos,
                req.RFC,
                req.Calle,
                req.CodigoPostal,
                req.Colonia,
                req.Ciudad,
                req.Estado,
                req.Municipio,
                req.CodigoBarrasCliente,
                Factura = req.Factura ? 1 : 0,
                req.IdVendedor
            });
    }

    /// <summary>
    /// Cierra nota de apartado: actualiza campos finales y ejecuta sp_DardeBaja.
    /// Equivale a: cmdCerrarNota_Click en VB6.
    /// </summary>
    public async Task<int> CerrarNotaAsync(CerrarApartadoRequest req)
    {
        using var db = CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();
        try
        {
            // Validar que tenga nombre de cliente
            if (string.IsNullOrWhiteSpace(req.NombreCliente))
                throw new InvalidOperationException("Falta el Nombre del Cliente.");

            // Validar que haya piezas
            var totalPiezas = await db.QueryFirstAsync<decimal>(
                "SELECT ISNULL(SUM(Total), 0) FROM PiezasNotasApartado WHERE IdNota = @Id",
                new { Id = req.IdNota }, tx);
            if (totalPiezas == 0)
                throw new InvalidOperationException("No se ha capturado ninguna pieza.");

            // Calcular total
            decimal total = req.Total ?? totalPiezas;

            // Validar que el pago cubra el total
            var totalPagado = await db.QueryFirstAsync<decimal>(
                "SELECT ISNULL(SUM(Importe), 0) FROM PagosNotasApartado WHERE IdNota = @Id",
                new { Id = req.IdNota }, tx);
            if (totalPagado < total)
                throw new InvalidOperationException("Todavía no ha sido cubierto el total de la nota.");

            // Actualizar campos finales de la nota
            await db.ExecuteAsync(
                @"UPDATE NotasApartado SET
                    NombreCliente = @NombreCliente, Telefonos = @Telefonos,
                    Factura = @Factura, Descuento = @Descuento,
                    Neto = @Total, Total = @Total, Bruto = @Bruto,
                    FormaPago = @FormaPago, IdVendedor = @IdVendedor
                  WHERE IdNota = @IdNota",
                new
                {
                    req.IdNota,
                    req.NombreCliente,
                    req.Telefonos,
                    Factura = req.Factura ? 1 : 0,
                    Descuento = req.Descuento ?? 0,
                    Total = total,
                    Bruto = totalPiezas,
                    req.FormaPago,
                    req.IdVendedor
                }, tx);

            // Ejecutar sp_DardeBaja (da de baja las piezas y mueve a tablas definitivas)
            await db.ExecuteAsync("EXEC sp_DardeBaja @IdNota",
                new { IdNota = req.IdNota }, tx);

            tx.Commit();
            _logger.LogInformation("Nota apartado cerrada: IdNota={IdNota}, Total={Total}", req.IdNota, total);
            return req.IdNota;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  CATÁLOGOS AUXILIARES
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Lista catálogo de piezas repetidas para el dropdown
    /// </summary>
    public async Task<List<RepetidaCatalogo>> ObtenerRepetidasAsync()
    {
        using var db = CreateConnection();
        return (await db.QueryAsync<RepetidaCatalogo>(
            @"SELECT TOP 200 CR.CodigoBarras, CR.Descripcion,
                     ISNULL(CR.Kilates,'') AS Kilates, CR.Precio, D.Divisor
              FROM CatalogoRepetidas CR
              INNER JOIN Divisores D ON CR.IdDivisor = D.IdDivisor
              ORDER BY CR.Descripcion")).ToList();
    }

    /// <summary>
    /// Busca colonias por código postal (sp_getcolonia)
    /// </summary>
    public async Task<List<dynamic>> BuscarColoniasAsync(string codigoPostal)
    {
        using var db = CreateConnection();
        return (await db.QueryAsync("EXEC sp_getcolonia @CP", new { CP = codigoPostal })).ToList();
    }

    /// <summary>
    /// Lista usuarios de Diamonds para dropdown
    /// </summary>
    public async Task<List<(int Id, string Nombre)>> ObtenerUsuariosAsync()
    {
        using var db = CreateConnection();
        var result = await db.QueryAsync<dynamic>(
            "SELECT TOP 50 IdUsuario AS Id, Nombre FROM Usuarios WHERE Activo <> 0 ORDER BY Nombre");
        return result.Select(r => ((int)r.Id, (string)r.Nombre)).ToList();
    }
}
