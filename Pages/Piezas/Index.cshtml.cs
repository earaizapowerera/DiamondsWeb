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

    public async Task<IActionResult> OnPostEliminarAsync(string codigoBarras, int? idRemision)
    {
        var ok = await _svc.EliminarPiezaAsync(codigoBarras, 1);
        TempData["MensajeExito"] = ok
            ? $"Pieza {codigoBarras} eliminada correctamente"
            : $"Error al eliminar pieza {codigoBarras}";
        return RedirectToPage(new { IdRemision = idRemision });
    }
}
