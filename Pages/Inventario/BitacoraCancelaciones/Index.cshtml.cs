using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Inventario.BitacoraCancelaciones;

[Authorize]
public class IndexModel : PageModel
{
    private readonly InventoryService _inventoryService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(InventoryService inventoryService, ILogger<IndexModel> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    public List<PiezaCancelada> Cancelaciones { get; set; } = new();

    public string? Buscar { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }

    public async Task OnGetAsync(string? buscar, DateTime? desde, DateTime? hasta)
    {
        Buscar = buscar;
        Desde = desde;
        Hasta = hasta;

        try
        {
            Cancelaciones = await _inventoryService.ObtenerPiezasCanceladasAsync(buscar, desde, hasta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar bitacora de cancelaciones");
            TempData["Error"] = $"Error al cargar bitacora: {ex.Message}";
        }
    }
}
