using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Catalogos.DefaultsUtilidadExtra;

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

    // -- Crear --
    [BindProperty]
    public decimal NuevaUtilidadExtra { get; set; }

    // -- Editar --
    [BindProperty]
    public int? EditId { get; set; }

    [BindProperty]
    public decimal? EditUtilidadExtra { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Defaults = await _catalogService.ObtenerDefaultsUtilidadExtraAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar defaults de utilidad extra");
            TempData["Error"] = $"Error al cargar datos: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            if (NuevaUtilidadExtra <= 0)
            {
                TempData["Error"] = "La utilidad extra debe ser mayor a 0.";
                return RedirectToPage();
            }

            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid : 1;
            await _catalogService.CrearDefaultUtilidadExtraAsync(NuevaUtilidadExtra, idUsuario);
            TempData["Success"] = "Utilidad extra creada exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear default de utilidad extra");
            TempData["Error"] = $"Error al crear: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditAsync()
    {
        try
        {
            if (EditId == null || EditUtilidadExtra == null)
            {
                TempData["Error"] = "Datos incompletos para editar.";
                return RedirectToPage();
            }

            if (EditUtilidadExtra <= 0)
            {
                TempData["Error"] = "La utilidad extra debe ser mayor a 0.";
                return RedirectToPage();
            }

            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid : 1;
            await _catalogService.ActualizarDefaultUtilidadExtraAsync(
                EditId.Value, EditUtilidadExtra.Value, idUsuario);
            TempData["Success"] = "Utilidad extra actualizada exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar default de utilidad extra");
            TempData["Error"] = $"Error al actualizar: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            await _catalogService.EliminarDefaultUtilidadExtraAsync(id);
            TempData["Success"] = "Utilidad extra eliminada exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar default de utilidad extra {Id}", id);
            TempData["Error"] = $"Error al eliminar: {ex.Message}";
        }

        return RedirectToPage();
    }
}
