using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Configuracion.DefaultsUtilidad;

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

    public List<DefaultUtilidad> Defaults { get; set; } = new();

    [BindProperty] public decimal NuevoUtilidad { get; set; }
    [BindProperty] public decimal? NuevoUtilidadReloj { get; set; }
    [BindProperty] public decimal? NuevoUtilidadGemas { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Defaults = await _catalogService.ObtenerDefaultsUtilidadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar defaults utilidad");
            TempData["Error"] = $"Error al cargar defaults utilidad: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid
                : throw new UnauthorizedAccessException("IdUsuario claim not found");
            await _catalogService.CrearDefaultUtilidadAsync(NuevoUtilidad, NuevoUtilidadReloj, NuevoUtilidadGemas, idUsuario);
            TempData["Success"] = "Default utilidad creado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear default utilidad");
            TempData["Error"] = $"Error al crear default utilidad: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            await _catalogService.EliminarDefaultUtilidadAsync(id);
            TempData["Success"] = "Default utilidad eliminado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar default utilidad {Id}", id);
            TempData["Error"] = $"Error al eliminar default utilidad: {ex.Message}";
        }

        return RedirectToPage();
    }
}
