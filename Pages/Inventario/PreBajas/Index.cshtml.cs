using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Inventario.PreBajas;

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

    public List<PreBaja> Items { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    /// <summary>Código de barras escaneado o tecleado para agregar.</summary>
    [BindProperty]
    public string NuevoCodigoBarras { get; set; } = "";

    /// <summary>1 = Venta, 2 = Devolución.</summary>
    [BindProperty]
    public int TipoBaja { get; set; } = 1;

    public async Task OnGetAsync()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(Buscar))
                Items = await _inventoryService.BuscarPreBajaAsync(Buscar.Trim());
            else
                Items = await _inventoryService.ObtenerPreBajasDelDiaAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar pre-bajas");
            TempData["Error"] = $"Error al cargar pre-bajas: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            await _inventoryService.RegistrarPreBajaAsync(NuevoCodigoBarras.Trim(), TipoBaja);
            TempData["Success"] = $"Pre-baja registrada: {NuevoCodigoBarras.Trim()}";
        }
        catch (ArgumentException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar pre-baja");
            TempData["Error"] = $"Error al registrar pre-baja: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string codigoBarras, DateTime fechaCaptura)
    {
        try
        {
            await _inventoryService.EliminarPreBajaAsync(codigoBarras, fechaCaptura);
            TempData["Success"] = $"Pre-baja eliminada: {codigoBarras}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar pre-baja {Codigo}", codigoBarras);
            TempData["Error"] = $"Error al eliminar: {ex.Message}";
        }

        return RedirectToPage();
    }
}
