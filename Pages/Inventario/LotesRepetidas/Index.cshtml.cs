using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Inventario.LotesRepetidas;

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

    public List<LoteRepetida> Lotes { get; set; } = new();
    public List<CatalogoRepetida> CatalogoRepetidas { get; set; } = new();
    public List<Moneda> Monedas { get; set; } = new();

    // Campos para nuevo lote
    [BindProperty]
    public string? NuevoCodigoBarras { get; set; }

    [BindProperty]
    public int NuevaCantidad { get; set; } = 1;

    [BindProperty]
    public decimal NuevoCostoBruto { get; set; }

    [BindProperty]
    public decimal NuevoDescuento { get; set; }

    [BindProperty]
    public decimal NuevoCostoNeto { get; set; }

    [BindProperty]
    public decimal NuevaUtilidad { get; set; }

    [BindProperty]
    public decimal NuevaUtilidadExtra { get; set; }

    [BindProperty]
    public decimal NuevoImpuesto { get; set; }

    [BindProperty]
    public decimal NuevoDivisor { get; set; } = 1;

    [BindProperty]
    public int? NuevoIdMoneda { get; set; }

    [BindProperty]
    public decimal NuevoTCCosto { get; set; } = 1;

    [BindProperty]
    public decimal NuevoTCCotizacion { get; set; } = 1;

    public async Task OnGetAsync()
    {
        try
        {
            Lotes = await _inventoryService.ObtenerLotesRepetidasAsync();
            CatalogoRepetidas = await _catalogService.ObtenerCatalogoRepetidasAsync();
            Monedas = await _catalogService.ObtenerMonedasAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar lotes de repetidas");
            TempData["Error"] = $"Error al cargar lotes: {ex.Message}";
        }
    }
}
