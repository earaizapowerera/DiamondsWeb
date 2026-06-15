using DiamondsWeb.Extensions;
using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Configuracion.DefaultsImpuesto;

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

    public List<DefaultFactorComun> Defaults { get; set; } = new();

    [BindProperty] public decimal NuevoImpuesto { get; set; }
    [BindProperty] public decimal NuevoDivisor { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Defaults = await _catalogService.ObtenerDefaultsFactorComunesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar defaults impuesto/divisor");
            TempData["Error"] = $"Error al cargar defaults: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            var idUsuario = User.GetRequiredIdUsuario();
            await _catalogService.CrearDefaultFactorComunAsync(NuevoImpuesto, NuevoDivisor, idUsuario);
            TempData["Success"] = "Default creado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear default");
            TempData["Error"] = $"Error al crear default: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            await _catalogService.EliminarDefaultFactorComunAsync(id);
            TempData["Success"] = "Default eliminado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar default {Id}", id);
            TempData["Error"] = $"Error al eliminar default: {ex.Message}";
        }

        return RedirectToPage();
    }
}
