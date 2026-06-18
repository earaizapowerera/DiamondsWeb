using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DiamondsWeb.Models;
using DiamondsWeb.Services;

namespace DiamondsWeb.Pages.Ventas.Apartados;

[Authorize]
[IgnoreAntiforgeryToken]
public class IndexModel : PageModel
{
    private readonly ApartadoService _svc;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ApartadoService svc, ILogger<IndexModel> logger)
    {
        _svc = svc;
        _logger = logger;
    }

    // ─── Datos para la vista ───────────────────────────────────
    public List<ApartadoSesion> Sesiones { get; set; } = [];
    public List<OpcionPagoPOS> OpcionesPago { get; set; } = [];

    [TempData] public string? MensajeError { get; set; }
    [TempData] public string? MensajeExito { get; set; }

    // ─── GET: Carga inicial ────────────────────────────────────
    public async Task OnGetAsync()
    {
        Sesiones = await _svc.ObtenerSesionesAbiertasAsync();
        OpcionesPago = await _svc.ObtenerOpcionesPagoAsync();
    }

    // ═══════════════════════════════════════════════════════════════
    //  AJAX Handlers (JSON API endpoints)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>GET ?handler=Sesiones — lista sesiones abiertas</summary>
    public async Task<IActionResult> OnGetSesionesAsync()
    {
        var sesiones = await _svc.ObtenerSesionesAbiertasAsync();
        return new JsonResult(new { ok = true, sesiones });
    }

    /// <summary>GET ?handler=Sesion&idNota=X — datos de una sesión</summary>
    public async Task<IActionResult> OnGetSesionAsync(int idNota)
    {
        var sesion = await _svc.ObtenerSesionAsync(idNota);
        if (sesion == null)
            return new JsonResult(new { ok = false, error = "Sesión no encontrada" });

        var piezas = await _svc.ObtenerPiezasAsync(idNota);
        var pagos = await _svc.ObtenerPagosAsync(idNota);
        var resumen = await _svc.CalcularResumenAsync(idNota, sesion.Descuento, 0, sesion.Factura);

        return new JsonResult(new { ok = true, sesion, piezas, pagos, resumen });
    }

    /// <summary>POST ?handler=CrearSesion — nueva sesión de apartado</summary>
    public async Task<IActionResult> OnPostCrearSesionAsync([FromBody] CrearApartadoSesionRequest req)
    {
        try
        {
            var sesion = await _svc.CrearSesionAsync(req);
            return new JsonResult(new { ok = true, sesion });
        }
        catch (InvalidOperationException ex)
        {
            return new JsonResult(new { ok = false, error = ex.Message });
        }
    }

    /// <summary>POST ?handler=CancelarSesion — cancela sesión</summary>
    public async Task<IActionResult> OnPostCancelarSesionAsync([FromBody] CancelarApartadoReq req)
    {
        await _svc.CancelarSesionAsync(req.IdNota);
        return new JsonResult(new { ok = true });
    }

    /// <summary>GET ?handler=BuscarPieza&cb=X — busca pieza</summary>
    public async Task<IActionResult> OnGetBuscarPiezaAsync(string cb)
    {
        var pieza = await _svc.BuscarPiezaAsync(cb);
        if (pieza == null)
            return new JsonResult(new { ok = false, error = "No existe la pieza. Intente de nuevo." });
        return new JsonResult(new { ok = true, pieza });
    }

    /// <summary>POST ?handler=AgregarPieza — agrega pieza a la nota</summary>
    public async Task<IActionResult> OnPostAgregarPiezaAsync([FromBody] AgregarPiezaApartadoRequest req)
    {
        try
        {
            var pieza = await _svc.AgregarPiezaAsync(req);
            var piezas = await _svc.ObtenerPiezasAsync(req.IdNota);
            var resumen = await _svc.CalcularResumenAsync(req.IdNota, 0, 0, req.EsFactura);
            return new JsonResult(new { ok = true, pieza, piezas, resumen });
        }
        catch (InvalidOperationException ex)
        {
            return new JsonResult(new { ok = false, error = ex.Message });
        }
    }

    /// <summary>POST ?handler=EliminarPieza — elimina pieza de la nota</summary>
    public async Task<IActionResult> OnPostEliminarPiezaAsync([FromBody] EliminarPiezaApartadoReq req)
    {
        await _svc.EliminarPiezaAsync(req.IdNota, req.CodigoBarras);
        var piezas = await _svc.ObtenerPiezasAsync(req.IdNota);
        var resumen = await _svc.CalcularResumenAsync(req.IdNota, req.DescuentoPct, req.SobrePrecio, req.EsFactura);
        return new JsonResult(new { ok = true, piezas, resumen });
    }

    /// <summary>GET ?handler=OpcionesPago — catálogo de opciones de pago</summary>
    public async Task<IActionResult> OnGetOpcionesPagoAsync()
    {
        var opciones = await _svc.ObtenerOpcionesPagoAsync();
        return new JsonResult(new { ok = true, opciones });
    }

    /// <summary>POST ?handler=RegistrarPago — registra pago</summary>
    public async Task<IActionResult> OnPostRegistrarPagoAsync([FromBody] RegistrarPagoApartadoRequest req)
    {
        try
        {
            await _svc.RegistrarPagoAsync(req);
            var pagos = await _svc.ObtenerPagosAsync(req.IdNota);
            var resumen = await _svc.CalcularResumenAsync(req.IdNota);
            return new JsonResult(new { ok = true, pagos, resumen });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { ok = false, error = ex.Message });
        }
    }

    /// <summary>POST ?handler=EliminarPago — elimina pago</summary>
    public async Task<IActionResult> OnPostEliminarPagoAsync([FromBody] EliminarPagoApartadoReq req)
    {
        await _svc.EliminarPagoAsync(req.IdNota, req.IdOpcionPago, req.Importe);
        var pagos = await _svc.ObtenerPagosAsync(req.IdNota);
        var resumen = await _svc.CalcularResumenAsync(req.IdNota);
        return new JsonResult(new { ok = true, pagos, resumen });
    }

    /// <summary>GET ?handler=Resumen — calcula totales</summary>
    public async Task<IActionResult> OnGetResumenAsync(int idNota, decimal descuento = 0, decimal sobrePrecio = 0, bool factura = false)
    {
        var resumen = await _svc.CalcularResumenAsync(idNota, descuento, sobrePrecio, factura);
        return new JsonResult(new { ok = true, resumen });
    }

    /// <summary>POST ?handler=ActualizarNota — actualiza campos del cliente/nota</summary>
    public async Task<IActionResult> OnPostActualizarNotaAsync([FromBody] ActualizarApartadoNotaReq req)
    {
        await _svc.ActualizarNotaAsync(req);
        return new JsonResult(new { ok = true });
    }

    /// <summary>POST ?handler=CerrarNota — cierra nota de apartado (sp_DardeBaja)</summary>
    public async Task<IActionResult> OnPostCerrarNotaAsync([FromBody] CerrarApartadoRequest req)
    {
        try
        {
            var idNota = await _svc.CerrarNotaAsync(req);
            return new JsonResult(new { ok = true, idNota });
        }
        catch (InvalidOperationException ex)
        {
            return new JsonResult(new { ok = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cerrando nota apartado {IdNota}", req.IdNota);
            return new JsonResult(new { ok = false, error = "Error al cerrar la nota: " + ex.Message });
        }
    }

    /// <summary>GET ?handler=Repetidas — catálogo de repetidas</summary>
    public async Task<IActionResult> OnGetRepetidasAsync()
    {
        var repetidas = await _svc.ObtenerRepetidasAsync();
        return new JsonResult(new { ok = true, repetidas });
    }

    /// <summary>GET ?handler=Colonias&cp=X — busca colonias por CP</summary>
    public async Task<IActionResult> OnGetColoniasAsync(string cp)
    {
        if (string.IsNullOrEmpty(cp) || cp.Length != 5)
            return new JsonResult(new { ok = false, error = "Código postal inválido" });

        var colonias = await _svc.BuscarColoniasAsync(cp);
        return new JsonResult(new { ok = true, colonias });
    }

    /// <summary>GET ?handler=Usuarios — lista usuarios Diamonds</summary>
    public async Task<IActionResult> OnGetUsuariosAsync()
    {
        var usuarios = await _svc.ObtenerUsuariosAsync();
        var result = usuarios.Select(u => new { id = u.Id, nombre = u.Nombre });
        return new JsonResult(new { ok = true, usuarios = result });
    }
}
