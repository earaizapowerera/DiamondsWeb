using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Piezas;

[Authorize]
public class IndexModel : PageModel
{
    private readonly PiezaService _svc;

    public IndexModel(PiezaService svc) => _svc = svc;

    [BindProperty(SupportsGet = true)] public string? Buscar { get; set; }
    [BindProperty(SupportsGet = true)] public int? IdRemision { get; set; }
    [BindProperty(SupportsGet = true)] public int? IdGrupo { get; set; }

    public List<PiezaResumen> Piezas { get; set; } = new();
    public List<Remision> Remisiones { get; set; } = new();
    public List<GrupoPieza> Grupos { get; set; } = new();
    public Remision? RemisionActual { get; set; }
    public RemisionTotales? Totales { get; set; }
    public string? MensajeExito { get; set; }

    public async Task OnGetAsync()
    {
        MensajeExito = TempData["MensajeExito"] as string;
        Grupos = await _svc.ObtenerGruposAsync();
        Remisiones = await _svc.ObtenerRemisionesAsync();

        if (IdRemision.HasValue)
        {
            RemisionActual = await _svc.ObtenerRemisionAsync(IdRemision.Value);
            Piezas = await _svc.ObtenerPiezasPorRemisionAsync(IdRemision.Value);
            Totales = await _svc.ObtenerTotalesRemisionAsync(IdRemision.Value);
        }
        else if (!string.IsNullOrWhiteSpace(Buscar) || IdGrupo.HasValue)
        {
            Piezas = await _svc.BuscarPiezasAsync(Buscar, null, IdGrupo);
        }
        else
        {
            Piezas = await _svc.BuscarPiezasAsync(null, null, null);
        }
    }

    /// <summary>
    /// AJAX: Verifica permisos de eliminacion para una pieza.
    /// Retorna JSON con el resultado del chequeo de permisos.
    /// </summary>
    public async Task<IActionResult> OnGetVerificarPermisoEliminarAsync(string codigoBarras)
    {
        if (string.IsNullOrWhiteSpace(codigoBarras))
            return new JsonResult(new { success = false, error = "Codigo de barras requerido." });

        var idUsuario = ObtenerIdUsuario();
        var resultado = await _svc.VerificarPermisoEliminarAsync(codigoBarras, idUsuario);
        return new JsonResult(resultado);
    }

    /// <summary>
    /// AJAX: Elimina una pieza con validacion de permisos.
    /// Si requiere autorizacion, valida credenciales de supervisor.
    /// </summary>
    public async Task<IActionResult> OnPostEliminarConPermisoAsync(
        [FromBody] EliminarPiezaRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.CodigoBarras))
            return new JsonResult(new EliminarPiezaResult
            {
                Success = false,
                Error = "Codigo de barras requerido."
            });

        var idUsuario = ObtenerIdUsuario();
        var idTienda = ObtenerIdTienda();

        var resultado = await _svc.EliminarPiezaConPermisoAsync(
            request.CodigoBarras,
            idUsuario,
            idTienda,
            request.Motivo,
            request.SupervisorNombre,
            request.SupervisorPassword);

        return new JsonResult(resultado);
    }

    private int ObtenerIdUsuario()
    {
        return int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid : 1;
    }

    private int ObtenerIdTienda()
    {
        return int.TryParse(User.FindFirst("IdTienda")?.Value, out var tid) ? tid : 1;
    }
}
