using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Procesos.ActualizacionRemisiones;

[Authorize]
public class IndexModel : PageModel
{
    private readonly SalesService _salesService;
    private readonly InventoryService _inventoryService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(SalesService salesService, InventoryService inventoryService, ILogger<IndexModel> logger)
    {
        _salesService = salesService;
        _inventoryService = inventoryService;
        _logger = logger;
    }

    public List<Remision> Remisiones { get; set; } = new();
    public List<Pieza> PiezasDisponibles { get; set; } = new();
    public List<Pieza> PiezasAsignadas { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? RemisionSeleccionada { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? ProveedorFiltro { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Remisiones = await _salesService.ObtenerRemisionesAsync(ProveedorFiltro);

            if (!string.IsNullOrEmpty(RemisionSeleccionada))
            {
                // Get all pieces - those without remision are available, those with this remision are assigned
                var todasPiezas = await _inventoryService.ObtenerPiezasSencillasAsync();
                PiezasDisponibles = todasPiezas.Where(p => p.IdRemision == null || p.IdRemision == 0).ToList();
                PiezasAsignadas = todasPiezas
                    .Where(p => p.IdRemision != null && p.IdRemision.ToString() == RemisionSeleccionada)
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar actualización de remisiones");
            TempData["Error"] = $"Error al cargar datos: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostAsignarAsync(string codigoBarras, string idRemision)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(codigoBarras) || string.IsNullOrWhiteSpace(idRemision))
            {
                TempData["Error"] = "Debe seleccionar una pieza y una remisión.";
                return RedirectToPage(new { RemisionSeleccionada = idRemision });
            }

            await _salesService.AsignarPiezaRemisionAsync(codigoBarras, idRemision);
            TempData["Success"] = $"Pieza {codigoBarras} asignada a remisión {idRemision}.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al asignar pieza a remisión");
            TempData["Error"] = $"Error al asignar pieza: {ex.Message}";
        }

        return RedirectToPage(new { RemisionSeleccionada = idRemision });
    }

    public async Task<IActionResult> OnPostDesasignarAsync(string codigoBarras, string idRemision)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(codigoBarras))
            {
                TempData["Error"] = "Debe seleccionar una pieza para desasignar.";
                return RedirectToPage(new { RemisionSeleccionada = idRemision });
            }

            // Desasignar by setting remision to empty/null
            await _salesService.AsignarPiezaRemisionAsync(codigoBarras, "");
            TempData["Success"] = $"Pieza {codigoBarras} desasignada de remisión.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desasignar pieza de remisión");
            TempData["Error"] = $"Error al desasignar pieza: {ex.Message}";
        }

        return RedirectToPage(new { RemisionSeleccionada = idRemision });
    }
}
