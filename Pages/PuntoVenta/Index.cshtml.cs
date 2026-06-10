using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DiamondsWeb.Models;
using DiamondsWeb.Services;

namespace DiamondsWeb.Pages.PuntoVenta;

[Authorize]
[IgnoreAntiforgeryToken]
public class IndexModel : PageModel
{
    private readonly PuntoVentaService _pos;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(PuntoVentaService pos, ILogger<IndexModel> logger)
    {
        _pos = pos;
        _logger = logger;
    }

    // ─── Datos para la vista ───────────────────────────────────
    public List<NotaSesion> Sesiones { get; set; } = [];
    public NotaSesion? SesionActual { get; set; }
    public List<PiezaTemporal> Piezas { get; set; } = [];
    public List<PagoNotaDetalle> Pagos { get; set; } = [];
    public List<OpcionPagoPOS> OpcionesPago { get; set; } = [];
    public ResumenNota? Resumen { get; set; }

    [TempData] public string? MensajeError { get; set; }
    [TempData] public string? MensajeExito { get; set; }

    // ─── GET: Carga inicial ────────────────────────────────────
    public async Task OnGetAsync()
    {
        Sesiones = await _pos.ObtenerSesionesAbiertasAsync();
        OpcionesPago = await _pos.ObtenerOpcionesPagoAsync();
    }

    // ═══════════════════════════════════════════════════════════════
    //  AJAX Handlers (JSON API endpoints)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>GET /PuntoVenta?handler=Sesiones — lista sesiones abiertas</summary>
    public async Task<IActionResult> OnGetSesionesAsync()
    {
        var sesiones = await _pos.ObtenerSesionesAbiertasAsync();
        return new JsonResult(new { ok = true, sesiones });
    }

    /// <summary>GET /PuntoVenta?handler=Sesion&idNota=X — datos de una sesión</summary>
    public async Task<IActionResult> OnGetSesionAsync(int idNota)
    {
        var sesion = await _pos.ObtenerSesionAsync(idNota);
        if (sesion == null)
            return new JsonResult(new { ok = false, error = "Sesión no encontrada" });

        var piezas = await _pos.ObtenerPiezasTemporalesAsync(idNota);
        var pagos = await _pos.ObtenerPagosAsync(idNota);
        var resumen = await _pos.CalcularResumenAsync(idNota, sesion.Descuento, 0, sesion.Factura);

        return new JsonResult(new { ok = true, sesion, piezas, pagos, resumen });
    }

    /// <summary>POST /PuntoVenta?handler=CrearSesion — nueva sesión de venta</summary>
    public async Task<IActionResult> OnPostCrearSesionAsync([FromBody] CrearSesionRequest req)
    {
        try
        {
            var sesion = await _pos.CrearSesionAsync(req);
            return new JsonResult(new { ok = true, sesion });
        }
        catch (InvalidOperationException ex)
        {
            return new JsonResult(new { ok = false, error = ex.Message });
        }
    }

    /// <summary>POST /PuntoVenta?handler=CancelarSesion — cancela sesión</summary>
    public async Task<IActionResult> OnPostCancelarSesionAsync([FromBody] CancelarSesionReq req)
    {
        await _pos.CancelarSesionAsync(req.IdNota);
        return new JsonResult(new { ok = true });
    }

    /// <summary>GET /PuntoVenta?handler=BuscarPieza&cb=X — busca pieza</summary>
    public async Task<IActionResult> OnGetBuscarPiezaAsync(string cb)
    {
        var pieza = await _pos.BuscarPiezaAsync(cb);
        if (pieza == null)
            return new JsonResult(new { ok = false, error = "No existe la pieza. Intente de nuevo." });
        return new JsonResult(new { ok = true, pieza });
    }

    /// <summary>POST /PuntoVenta?handler=AgregarPieza — agrega pieza a la nota</summary>
    public async Task<IActionResult> OnPostAgregarPiezaAsync([FromBody] AgregarPiezaRequest req)
    {
        try
        {
            var pieza = await _pos.AgregarPiezaAsync(req);
            var piezas = await _pos.ObtenerPiezasTemporalesAsync(req.IdNota);
            var resumen = await _pos.CalcularResumenAsync(req.IdNota, 0, 0, req.EsFactura);
            return new JsonResult(new { ok = true, pieza, piezas, resumen });
        }
        catch (InvalidOperationException ex)
        {
            return new JsonResult(new { ok = false, error = ex.Message });
        }
    }

    /// <summary>POST /PuntoVenta?handler=EliminarPieza — elimina pieza de la nota</summary>
    public async Task<IActionResult> OnPostEliminarPiezaAsync([FromBody] EliminarPiezaReq req)
    {
        await _pos.EliminarPiezaAsync(req.IdNota, req.CodigoBarras);
        var piezas = await _pos.ObtenerPiezasTemporalesAsync(req.IdNota);
        var resumen = await _pos.CalcularResumenAsync(req.IdNota, req.DescuentoPct, req.SobrePrecio, req.EsFactura);
        return new JsonResult(new { ok = true, piezas, resumen });
    }

    /// <summary>GET /PuntoVenta?handler=OpcionesPago — catálogo de opciones de pago</summary>
    public async Task<IActionResult> OnGetOpcionesPagoAsync()
    {
        var opciones = await _pos.ObtenerOpcionesPagoAsync();
        return new JsonResult(new { ok = true, opciones });
    }

    /// <summary>POST /PuntoVenta?handler=RegistrarPago — registra pago</summary>
    public async Task<IActionResult> OnPostRegistrarPagoAsync([FromBody] RegistrarPagoRequest req)
    {
        try
        {
            await _pos.RegistrarPagoAsync(req);
            var pagos = await _pos.ObtenerPagosAsync(req.IdNota);
            return new JsonResult(new { ok = true, pagos });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { ok = false, error = ex.Message });
        }
    }

    /// <summary>POST /PuntoVenta?handler=EliminarPago — elimina pago</summary>
    public async Task<IActionResult> OnPostEliminarPagoAsync([FromBody] EliminarPagoReq req)
    {
        await _pos.EliminarPagoAsync(req.IdNota, req.IdOpcionPago, req.Importe);
        var pagos = await _pos.ObtenerPagosAsync(req.IdNota);
        return new JsonResult(new { ok = true, pagos });
    }

    /// <summary>GET /PuntoVenta?handler=Resumen — calcula totales</summary>
    public async Task<IActionResult> OnGetResumenAsync(int idNota, decimal descuento = 0, decimal sobrePrecio = 0, bool factura = false)
    {
        var resumen = await _pos.CalcularResumenAsync(idNota, descuento, sobrePrecio, factura);
        return new JsonResult(new { ok = true, resumen });
    }

    /// <summary>POST /PuntoVenta?handler=ActualizarNota — actualiza campos de la nota</summary>
    public async Task<IActionResult> OnPostActualizarNotaAsync([FromBody] ActualizarNotaReq req)
    {
        await _pos.ActualizarNotaAsync(req.IdNota, req.NombreCliente, req.Telefonos,
            req.Comentarios, req.Factura, req.FechaBaja, req.IdVendedor);
        return new JsonResult(new { ok = true });
    }

    /// <summary>POST /PuntoVenta?handler=CerrarNota — cierra e imprime</summary>
    public async Task<IActionResult> OnPostCerrarNotaAsync([FromBody] CerrarNotaRequest req)
    {
        try
        {
            var idNota = await _pos.CerrarNotaAsync(req);
            return new JsonResult(new { ok = true, idNota });
        }
        catch (InvalidOperationException ex)
        {
            return new JsonResult(new { ok = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cerrando nota {IdNota}", req.IdNota);
            return new JsonResult(new { ok = false, error = "Error al cerrar la nota: " + ex.Message });
        }
    }

    /// <summary>GET /PuntoVenta?handler=NotaCerrada&idNota=X — datos para impresión</summary>
    public async Task<IActionResult> OnGetNotaCerradaAsync(int idNota)
    {
        var nota = await _pos.ObtenerNotaCerradaAsync(idNota);
        if (nota == null)
            return new JsonResult(new { ok = false, error = "Nota no encontrada" });
        return new JsonResult(new { ok = true, nota });
    }

    /// <summary>GET /PuntoVenta?handler=Usuarios — lista usuarios Diamonds</summary>
    public async Task<IActionResult> OnGetUsuariosAsync()
    {
        var usuarios = await _pos.ObtenerUsuariosAsync();
        var result = usuarios.Select(u => new { id = u.Id, nombre = u.Nombre });
        return new JsonResult(new { ok = true, usuarios = result });
    }

    // ─── Request DTOs internos ─────────────────────────────────
    public record CancelarSesionReq(int IdNota);
    public record EliminarPiezaReq(int IdNota, string CodigoBarras, decimal DescuentoPct = 0, decimal SobrePrecio = 0, bool EsFactura = false);
    public record EliminarPagoReq(int IdNota, int IdOpcionPago, decimal Importe);
    public record ActualizarNotaReq(int IdNota, string? NombreCliente = null, string? Telefonos = null,
        string? Comentarios = null, bool? Factura = null, DateTime? FechaBaja = null, int? IdVendedor = null);
}
