using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Procesos.ActualizacionFacturas;

[Authorize]
public class IndexModel : PageModel
{
    private readonly SalesService _salesService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(SalesService salesService, ILogger<IndexModel> logger)
    {
        _salesService = salesService;
        _logger = logger;
    }

    public List<Factura> Facturas { get; set; } = new();
    public List<PiezaActualizable> PiezasDisponibles { get; set; } = new();
    public List<PiezaActualizable> PiezasAsignadas { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? FacturaSeleccionada { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? ProveedorFiltro { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Facturas = await _salesService.ObtenerFacturasAsync(ProveedorFiltro);
            var todasPiezas = await _salesService.ObtenerPiezasParaActualizarAsync(FacturaSeleccionada);

            PiezasDisponibles = todasPiezas.Where(p => string.IsNullOrEmpty(p.IdFactura)).ToList();
            PiezasAsignadas = todasPiezas.Where(p => !string.IsNullOrEmpty(p.IdFactura) && p.IdFactura == FacturaSeleccionada).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar actualización de facturas");
            TempData["Error"] = $"Error al cargar datos: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostAsignarAsync(string codigoBarras, string idFactura,
        decimal? tcCosto, decimal? cbFactura, decimal? cnFactura)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(codigoBarras) || string.IsNullOrWhiteSpace(idFactura))
            {
                TempData["Error"] = "Debe seleccionar una pieza y una factura.";
                return RedirectToPage(new { FacturaSeleccionada = idFactura });
            }

            await _salesService.AsignarPiezaFacturaAsync(codigoBarras, idFactura, tcCosto, cbFactura, cnFactura);
            TempData["Success"] = $"Pieza {codigoBarras} asignada a factura {idFactura}.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al asignar pieza a factura");
            TempData["Error"] = $"Error al asignar pieza: {ex.Message}";
        }

        return RedirectToPage(new { FacturaSeleccionada = idFactura });
    }

    public async Task<IActionResult> OnPostDesasignarAsync(string codigoBarras, string idFactura)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(codigoBarras))
            {
                TempData["Error"] = "Debe seleccionar una pieza para desasignar.";
                return RedirectToPage(new { FacturaSeleccionada = idFactura });
            }

            await _salesService.DesasignarPiezaFacturaAsync(codigoBarras);
            TempData["Success"] = $"Pieza {codigoBarras} desasignada de factura.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desasignar pieza de factura");
            TempData["Error"] = $"Error al desasignar pieza: {ex.Message}";
        }

        return RedirectToPage(new { FacturaSeleccionada = idFactura });
    }
}
