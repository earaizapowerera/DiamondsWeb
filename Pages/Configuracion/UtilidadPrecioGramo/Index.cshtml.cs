using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Configuracion.UtilidadPrecioGramo;

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

    public List<UtilidadExtraPrecioGramo> Rangos { get; set; } = new();

    [BindProperty] public decimal NuevoDesde { get; set; }
    [BindProperty] public decimal NuevoHasta { get; set; }
    [BindProperty] public decimal NuevoUtilidadExtra { get; set; }

    // ── Edit fields ──
    [BindProperty] public int? EditId { get; set; }
    [BindProperty] public decimal EditDesde { get; set; }
    [BindProperty] public decimal EditHasta { get; set; }
    [BindProperty] public decimal EditUtilidadExtra { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Rangos = await _catalogService.ObtenerUtilidadExtraPrecioGramoAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar rangos utilidad precio/gramo");
            TempData["Error"] = $"Error al cargar rangos: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            if (NuevoDesde < 0 || NuevoHasta < 0)
            {
                TempData["Error"] = "Los valores de precio no pueden ser negativos.";
                return RedirectToPage();
            }

            if (NuevoDesde >= NuevoHasta)
            {
                TempData["Error"] = "El valor 'Desde' debe ser menor que 'Hasta'.";
                return RedirectToPage();
            }

            if (NuevoUtilidadExtra <= 0)
            {
                TempData["Error"] = "La utilidad extra debe ser mayor a 0.";
                return RedirectToPage();
            }

            if (await _catalogService.ExisteRangoSolapadoAsync(NuevoDesde, NuevoHasta))
            {
                TempData["Error"] = $"El rango {NuevoDesde:N2} - {NuevoHasta:N2} se solapa con un rango existente.";
                return RedirectToPage();
            }

            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid : 1;
            await _catalogService.CrearUtilidadExtraPrecioGramoAsync(NuevoDesde, NuevoHasta, NuevoUtilidadExtra, idUsuario);
            TempData["Success"] = "Rango creado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear rango");
            TempData["Error"] = $"Error al crear rango: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditAsync()
    {
        try
        {
            if (EditId == null)
            {
                TempData["Error"] = "Datos incompletos para editar.";
                return RedirectToPage();
            }

            if (EditDesde < 0 || EditHasta < 0)
            {
                TempData["Error"] = "Los valores de precio no pueden ser negativos.";
                return RedirectToPage();
            }

            if (EditDesde >= EditHasta)
            {
                TempData["Error"] = "El valor 'Desde' debe ser menor que 'Hasta'.";
                return RedirectToPage();
            }

            if (EditUtilidadExtra <= 0)
            {
                TempData["Error"] = "La utilidad extra debe ser mayor a 0.";
                return RedirectToPage();
            }

            if (await _catalogService.ExisteRangoSolapadoAsync(EditDesde, EditHasta, EditId.Value))
            {
                TempData["Error"] = $"El rango {EditDesde:N2} - {EditHasta:N2} se solapa con otro rango existente.";
                return RedirectToPage();
            }

            await _catalogService.ActualizarUtilidadExtraPrecioGramoAsync(EditId.Value, EditDesde, EditHasta, EditUtilidadExtra);
            TempData["Success"] = "Rango actualizado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar rango");
            TempData["Error"] = $"Error al actualizar rango: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            await _catalogService.EliminarUtilidadExtraPrecioGramoAsync(id);
            TempData["Success"] = "Rango eliminado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar rango {Id}", id);
            TempData["Error"] = $"Error al eliminar rango: {ex.Message}";
        }

        return RedirectToPage();
    }
}
