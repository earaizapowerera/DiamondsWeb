using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Procesos.PreBajas;

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

    public List<PreBaja> PreBajasHoy { get; set; } = new();

    [BindProperty]
    public string? CodigoBarras { get; set; }

    [BindProperty]
    public int TipoBaja { get; set; } = 1;

    public async Task OnGetAsync()
    {
        try
        {
            PreBajasHoy = await _inventoryService.ObtenerPreBajasDelDiaAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar pre-bajas");
            TempData["Error"] = $"Error al cargar pre-bajas: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostCrearAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(CodigoBarras))
            {
                TempData["Error"] = "Debe ingresar un código de barras.";
                return RedirectToPage();
            }

            await _inventoryService.RegistrarPreBajaAsync(CodigoBarras.Trim(), TipoBaja);
            TempData["Success"] = $"Pre-baja registrada para pieza {CodigoBarras} ({(TipoBaja == 1 ? "Venta" : "Devolución")}).";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear pre-baja");
            TempData["Error"] = $"Error al crear pre-baja: {ex.Message}";
        }

        return RedirectToPage();
    }
}
