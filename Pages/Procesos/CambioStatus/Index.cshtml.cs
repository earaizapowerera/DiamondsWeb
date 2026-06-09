using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Procesos.CambioStatus;

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

    public Pieza? PiezaEncontrada { get; set; }
    public List<StatusPieza> StatusDisponibles { get; set; } = new();
    public List<BitacoraStatus> Bitacora { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? CodigoBarras { get; set; }

    [BindProperty]
    public int NuevoStatus { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            StatusDisponibles = await _inventoryService.ObtenerStatusPiezasAsync();

            if (!string.IsNullOrWhiteSpace(CodigoBarras))
            {
                PiezaEncontrada = await _inventoryService.ObtenerPiezaAsync(CodigoBarras.Trim());
                if (PiezaEncontrada != null)
                {
                    Bitacora = await _inventoryService.ObtenerBitacoraStatusAsync(CodigoBarras.Trim());
                }
                else
                {
                    TempData["Error"] = $"Pieza '{CodigoBarras}' no encontrada.";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar cambio de status");
            TempData["Error"] = $"Error al cargar datos: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostCambiarAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(CodigoBarras))
            {
                TempData["Error"] = "Debe ingresar un código de barras.";
                return RedirectToPage();
            }

            if (NuevoStatus <= 0)
            {
                TempData["Error"] = "Debe seleccionar un status válido.";
                return RedirectToPage(new { CodigoBarras });
            }

            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid : 1;
            var resultado = await _inventoryService.CambiarStatusPiezaAsync(CodigoBarras.Trim(), NuevoStatus, idUsuario);
            TempData["Success"] = resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar status de pieza");
            TempData["Error"] = $"Error al cambiar status: {ex.Message}";
        }

        return RedirectToPage(new { CodigoBarras });
    }
}
