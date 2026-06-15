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

    public List<Models.DefaultUtilidadExtra> Defaults { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    [BindProperty]
    public decimal NuevoUtilidadExtra { get; set; }

    [BindProperty]
    public int? EditId { get; set; }

    [BindProperty]
    public decimal? EditUtilidadExtra { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Defaults = await _catalogService.ObtenerDefaultsUtilidadExtraAsync();
            if (!string.IsNullOrWhiteSpace(Buscar))
                Defaults = Defaults
                    .Where(d => d.DefaultUtilidadExtra1.ToString("0.000").Contains(Buscar, StringComparison.OrdinalIgnoreCase))
                    .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar defaults utilidad extra");
            TempData["Error"] = $"Error al cargar datos: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            if (NuevoUtilidadExtra <= 0)
            {
                TempData["Error"] = "La utilidad extra debe ser mayor a 0.";
                return RedirectToPage();
            }

            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid
                : throw new UnauthorizedAccessException("IdUsuario claim not found");
            await _catalogService.CrearDefaultUtilidadExtraAsync(NuevoUtilidadExtra, idUsuario);
            TempData["Success"] = "Default utilidad extra creado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear default utilidad extra");
            TempData["Error"] = $"Error al crear: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditAsync()
    {
        try
        {
            if (EditId == null || EditUtilidadExtra == null || EditUtilidadExtra <= 0)
            {
                TempData["Error"] = "Datos incompletos para editar.";
                return RedirectToPage();
            }

            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid
                : throw new UnauthorizedAccessException("IdUsuario claim not found");
            await _catalogService.ActualizarDefaultUtilidadExtraAsync(EditId.Value, EditUtilidadExtra.Value, idUsuario);
            TempData["Success"] = "Default utilidad extra actualizado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar default utilidad extra {Id}", EditId);
            TempData["Error"] = $"Error al actualizar: {ex.Message}";
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
            TempData["Error"] = $"Error al eliminar: {ex.Message}";
        }

        return RedirectToPage();
    }
}
