using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Catalogos.Monedas;

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

    public List<Moneda> Monedas { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    [BindProperty]
    public string NuevoNombre { get; set; } = "";

    [BindProperty]
    public bool NuevaExtranjera { get; set; }

    [BindProperty]
    public int? EditId { get; set; }

    [BindProperty]
    public string? EditNombre { get; set; }

    [BindProperty]
    public bool EditExtranjera { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Monedas = await _catalogService.ObtenerMonedasAsync();
            if (!string.IsNullOrWhiteSpace(Buscar))
                Monedas = Monedas.Where(m => m.Moneda1.Contains(Buscar, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar monedas");
            TempData["Error"] = $"Error al cargar monedas: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NuevoNombre))
            {
                TempData["Error"] = "El nombre de la moneda es requerido.";
                return RedirectToPage();
            }

            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid : 1;
            await _catalogService.CrearMonedaAsync(NuevoNombre.Trim(), NuevaExtranjera, idUsuario);
            TempData["Success"] = "Moneda creada exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear moneda");
            TempData["Error"] = $"Error al crear moneda: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditAsync()
    {
        try
        {
            if (EditId == null || string.IsNullOrWhiteSpace(EditNombre))
            {
                TempData["Error"] = "Datos incompletos para editar.";
                return RedirectToPage();
            }

            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid : 1;
            await _catalogService.ActualizarMonedaAsync(EditId.Value, EditNombre.Trim(), EditExtranjera, idUsuario);
            TempData["Success"] = "Moneda actualizada exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar moneda");
            TempData["Error"] = $"Error al actualizar moneda: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            await _catalogService.EliminarMonedaAsync(id);
            TempData["Success"] = "Moneda eliminada exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar moneda {Id}", id);
            TempData["Error"] = $"Error al eliminar moneda: {ex.Message}";
        }

        return RedirectToPage();
    }
}
