using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Configuracion.DefaultsUtilidadExtra;

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

    public List<DefaultUtilidadExtra> Defaults { get; set; } = new();

    [BindProperty] public decimal NuevoUtilidadExtra { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Defaults = await _catalogService.ObtenerDefaultsUtilidadExtraAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar defaults utilidad extra");
            TempData["Error"] = $"Error al cargar defaults utilidad extra: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid : 1;
            await _catalogService.CrearDefaultUtilidadExtraAsync(NuevoUtilidadExtra, idUsuario);
            TempData["Success"] = "Default utilidad extra creado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear default utilidad extra");
            TempData["Error"] = $"Error al crear default utilidad extra: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            await _catalogService.EliminarDefaultUtilidadExtraAsync(id);
            TempData["Success"] = "Default utilidad extra eliminado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar default utilidad extra {Id}", id);
            TempData["Error"] = $"Error al eliminar default utilidad extra: {ex.Message}";
        }

        return RedirectToPage();
    }
}
