using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Configuracion.DisenioEtiquetas;

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

    public List<DisenioEtiqueta> Etiquetas { get; set; } = new();

    public async Task OnGetAsync()
    {
        try
        {
            Etiquetas = await _catalogService.ObtenerDiseniosEtiquetasAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar disenos de etiquetas");
            TempData["Error"] = $"Error al cargar disenos de etiquetas: {ex.Message}";
        }
    }
}
