using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Catalogos.DefaultsFactorComunes;

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

    [BindProperty]
    public decimal NuevoImpuesto { get; set; }

    [BindProperty]
    public decimal NuevoDivisor { get; set; }

    [BindProperty]
    public int? EditId { get; set; }

    [BindProperty]
    public decimal? EditImpuesto { get; set; }

    [BindProperty]
    public decimal? EditDivisor { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Defaults = await _catalogService.ObtenerDefaultsFactorComunesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar defaults factor comunes");
            TempData["Error"] = $"Error al cargar datos: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            if (NuevoImpuesto <= 0 && NuevoDivisor <= 0)
            {
                TempData["Error"] = "Debe ingresar al menos un valor de impuesto o divisor.";
                return RedirectToPage();
            }

            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid
                : throw new UnauthorizedAccessException("IdUsuario claim not found");
            await _catalogService.CrearDefaultFactorComunAsync(NuevoImpuesto, NuevoDivisor, idUsuario);
            TempData["Success"] = "Default creado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear default factor comun");
            TempData["Error"] = $"Error al crear: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditAsync()
    {
        try
        {
            if (EditId == null || EditImpuesto == null || EditDivisor == null)
            {
                TempData["Error"] = "Datos incompletos para editar.";
                return RedirectToPage();
            }

            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid
                : throw new UnauthorizedAccessException("IdUsuario claim not found");
            await _catalogService.ActualizarDefaultFactorComunAsync(EditId.Value, EditImpuesto.Value, EditDivisor.Value, idUsuario);
            TempData["Success"] = "Default actualizado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar default factor comun");
            TempData["Error"] = $"Error al actualizar: {ex.Message}";
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
            TempData["Error"] = $"Error al eliminar: {ex.Message}";
        }

        return RedirectToPage();
    }
}
