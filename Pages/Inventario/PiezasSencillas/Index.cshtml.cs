using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Inventario.PiezasSencillas;

[Authorize]
public class IndexModel : PageModel
{
    private readonly InventoryService _inventoryService;
    private readonly CatalogService _catalogService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(InventoryService inventoryService, CatalogService catalogService, ILogger<IndexModel> logger)
    {
        _inventoryService = inventoryService;
        _catalogService = catalogService;
        _logger = logger;
    }

    public List<Pieza> Piezas { get; set; } = new();
    public List<Grupo> Grupos { get; set; } = new();
    public List<Proveedor> Proveedores { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? IdGrupo { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Proveedor { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Grupos = await _catalogService.ObtenerGruposAsync();
            Proveedores = await _catalogService.ObtenerProveedoresAsync();
            Piezas = await _inventoryService.ObtenerPiezasSencillasAsync(Buscar, IdGrupo, Proveedor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar piezas sencillas");
            TempData["Error"] = $"Error al cargar piezas: {ex.Message}";
        }
    }
}
