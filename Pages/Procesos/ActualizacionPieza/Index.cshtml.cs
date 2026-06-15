using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Procesos.ActualizacionPieza;

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

    public Pieza? PiezaEncontrada { get; set; }
    public List<Factura> Facturas { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? CodigoBarras { get; set; }

    [BindProperty]
    public string? IdFactura { get; set; }

    [BindProperty]
    public decimal? CBPieza { get; set; }

    [BindProperty]
    public decimal? CNPieza { get; set; }

    [BindProperty]
    public decimal? DescPieza { get; set; }

    [BindProperty]
    public decimal? CBFactura { get; set; }

    [BindProperty]
    public decimal? CNFactura { get; set; }

    [BindProperty]
    public decimal? DescFactura { get; set; }

    [BindProperty]
    public decimal? TCCosto { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Facturas = await _salesService.ObtenerFacturasAsync();

            if (!string.IsNullOrWhiteSpace(CodigoBarras))
            {
                PiezaEncontrada = await _inventoryService.ObtenerPiezaAsync(CodigoBarras.Trim());
                if (PiezaEncontrada == null)
                {
                    TempData["Error"] = $"Pieza '{CodigoBarras}' no encontrada.";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar pieza");
            TempData["Error"] = $"Error al buscar pieza: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostActualizarAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(CodigoBarras))
            {
                TempData["Error"] = "Debe ingresar un código de barras.";
                return RedirectToPage();
            }

            var pieza = await _inventoryService.ObtenerPiezaAsync(CodigoBarras.Trim());
            if (pieza == null)
            {
                TempData["Error"] = $"Pieza '{CodigoBarras}' no encontrada.";
                return RedirectToPage();
            }

            // Update cost fields
            pieza.CBPieza = CBPieza;
            pieza.CNPieza = CNPieza;
            pieza.DescPieza = DescPieza;
            pieza.CBFactura = CBFactura;
            pieza.CNFactura = CNFactura;
            pieza.DescFactura = DescFactura;
            pieza.TCCosto = TCCosto;

            if (!string.IsNullOrWhiteSpace(IdFactura))
            {
                await _salesService.AsignarPiezaFacturaAsync(CodigoBarras.Trim(), IdFactura, TCCosto, CBFactura, CNFactura);
            }

            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid
                : throw new UnauthorizedAccessException("IdUsuario claim not found");
            pieza.IdUsuario = idUsuario;
            await _inventoryService.ActualizarPiezaSencillaAsync(pieza);

            TempData["Success"] = $"Pieza {CodigoBarras} actualizada exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar pieza");
            TempData["Error"] = $"Error al actualizar pieza: {ex.Message}";
        }

        return RedirectToPage(new { CodigoBarras });
    }
}
