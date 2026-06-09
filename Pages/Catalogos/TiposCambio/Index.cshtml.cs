using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Catalogos.TiposCambio;

[Authorize]
public class IndexModel : PageModel
{
    private readonly CatalogService _catalogService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(CatalogService catalogService, ILogger<IndexModel> logger)
    {
        _catalogService = catalogService;
        _logger = logger;
    }

    public List<TipoCambio> TiposCambio { get; set; } = new();
    public List<Moneda> Monedas { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? FiltroMoneda { get; set; }

    [BindProperty]
    public int NuevoIdMoneda { get; set; }

    [BindProperty]
    public decimal NuevoCotizacion { get; set; }

    [BindProperty]
    public decimal NuevoVenta { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            TiposCambio = await _catalogService.ObtenerTiposCambioAsync();
            Monedas = await _catalogService.ObtenerMonedasAsync();

            if (FiltroMoneda.HasValue && FiltroMoneda > 0)
                TiposCambio = TiposCambio.Where(tc => tc.IdMoneda == FiltroMoneda.Value).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar tipos de cambio");
            TempData["Error"] = $"Error al cargar tipos de cambio: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            if (NuevoIdMoneda <= 0)
            {
                TempData["Error"] = "Debe seleccionar una moneda.";
                return RedirectToPage();
            }

            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid : 1;
            await _catalogService.CrearTipoCambioAsync(NuevoIdMoneda, NuevoCotizacion, NuevoVenta, idUsuario);
            TempData["Success"] = "Tipo de cambio registrado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear tipo de cambio");
            TempData["Error"] = $"Error al crear tipo de cambio: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            await _catalogService.EliminarTipoCambioAsync(id);
            TempData["Success"] = "Tipo de cambio eliminado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar tipo de cambio {Id}", id);
            TempData["Error"] = $"Error al eliminar tipo de cambio: {ex.Message}";
        }

        return RedirectToPage();
    }
}
