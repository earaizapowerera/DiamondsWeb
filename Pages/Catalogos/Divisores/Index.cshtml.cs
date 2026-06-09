using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Catalogos.Divisores;

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

    public List<Divisor> Divisores { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    [BindProperty]
    public string NuevaDescripcion { get; set; } = "";

    [BindProperty]
    public decimal NuevoValor { get; set; }

    [BindProperty]
    public int? EditId { get; set; }

    [BindProperty]
    public string? EditDescripcion { get; set; }

    [BindProperty]
    public decimal EditValor { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Divisores = await _catalogService.ObtenerDivisoresAsync();
            if (!string.IsNullOrWhiteSpace(Buscar))
                Divisores = Divisores
                    .Where(d => d.Descripcion.Contains(Buscar, StringComparison.OrdinalIgnoreCase))
                    .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar divisores");
            TempData["Error"] = $"Error al cargar divisores: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NuevaDescripcion))
            {
                TempData["Error"] = "La descripcion del divisor es requerida.";
                return RedirectToPage();
            }

            await _catalogService.CrearDivisorAsync(NuevaDescripcion.Trim(), NuevoValor);
            TempData["Success"] = "Divisor creado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear divisor");
            TempData["Error"] = $"Error al crear divisor: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditAsync()
    {
        try
        {
            if (EditId == null || string.IsNullOrWhiteSpace(EditDescripcion))
            {
                TempData["Error"] = "Datos incompletos para editar.";
                return RedirectToPage();
            }

            await _catalogService.ActualizarDivisorAsync(EditId.Value, EditDescripcion.Trim(), EditValor);
            TempData["Success"] = "Divisor actualizado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar divisor");
            TempData["Error"] = $"Error al actualizar divisor: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            await _catalogService.EliminarDivisorAsync(id);
            TempData["Success"] = "Divisor eliminado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar divisor {Id}", id);
            TempData["Error"] = $"Error al eliminar divisor: {ex.Message}";
        }

        return RedirectToPage();
    }
}
