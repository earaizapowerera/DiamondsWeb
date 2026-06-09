using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Catalogos.Diamantes;

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

    public List<DiamanteLista> Diamantes { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Diamantes = await _catalogService.ObtenerDiamantesAsync(Buscar);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar diamantes");
            TempData["Error"] = $"Error al cargar diamantes: {ex.Message}";
        }
    }
}
