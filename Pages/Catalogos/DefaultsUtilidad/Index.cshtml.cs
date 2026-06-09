using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Catalogos.DefaultsUtilidad;

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

    // ── Crear ──
    [BindProperty]
    public decimal NuevaUtilidad { get; set; }

    [BindProperty]
    public decimal NuevaUtilidadGemas { get; set; }

    [BindProperty]
    public decimal NuevaUtilidadReloj { get; set; }

    // ── Editar ──
    [BindProperty]
    public int? EditId { get; set; }

    [BindProperty]
    public decimal? EditUtilidad { get; set; }

    [BindProperty]
    public decimal? EditUtilidadGemas { get; set; }

    [BindProperty]
    public decimal? EditUtilidadReloj { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Defaults = await _catalogService.ObtenerDefaultsUtilidadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar defaults de utilidad");
            TempData["Error"] = $"Error al cargar datos: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            if (NuevaUtilidad < 0 || NuevaUtilidadGemas < 0 || NuevaUtilidadReloj < 0)
            {
                TempData["Error"] = "Los factores de utilidad no pueden ser negativos.";
                return RedirectToPage();
            }

            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid : 1;
            await _catalogService.CrearDefaultUtilidadAsync(
                NuevaUtilidad, NuevaUtilidadGemas, NuevaUtilidadReloj, idUsuario);
            TempData["Success"] = "Factor de utilidad creado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear default de utilidad");
            TempData["Error"] = $"Error al crear: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditAsync()
    {
        try
        {
            if (EditId == null || EditUtilidad == null || EditUtilidadGemas == null || EditUtilidadReloj == null)
            {
                TempData["Error"] = "Datos incompletos para editar.";
                return RedirectToPage();
            }

            if (EditUtilidad < 0 || EditUtilidadGemas < 0 || EditUtilidadReloj < 0)
            {
                TempData["Error"] = "Los factores de utilidad no pueden ser negativos.";
                return RedirectToPage();
            }

            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid : 1;
            await _catalogService.ActualizarDefaultUtilidadAsync(
                EditId.Value, EditUtilidad.Value, EditUtilidadGemas.Value, EditUtilidadReloj.Value, idUsuario);
            TempData["Success"] = "Factor de utilidad actualizado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar default de utilidad");
            TempData["Error"] = $"Error al actualizar: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            await _catalogService.EliminarDefaultUtilidadAsync(id);
            TempData["Success"] = "Factor de utilidad eliminado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar default de utilidad {Id}", id);
            TempData["Error"] = $"Error al eliminar: {ex.Message}";
        }

        return RedirectToPage();
    }
}
