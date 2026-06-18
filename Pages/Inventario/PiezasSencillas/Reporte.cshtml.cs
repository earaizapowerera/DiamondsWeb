using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Inventario.PiezasSencillas;

[Authorize]
public class ReporteModel : PageModel
{
    private readonly ReportePiezasService _reporteService;
    private readonly CatalogService _catalogService;
    private readonly ILogger<ReporteModel> _logger;

    public ReporteModel(
        ReportePiezasService reporteService,
        CatalogService catalogService,
        ILogger<ReporteModel> logger)
    {
        _reporteService = reporteService;
        _catalogService = catalogService;
        _logger = logger;
    }

    public List<PiezaReporte> Piezas { get; set; } = new();
    public TotalesPiezas Totales { get; set; } = new();
    public List<Grupo> Grupos { get; set; } = new();
    public List<Proveedor> Proveedores { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? IdGrupo { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Proveedor { get; set; }

    /// <summary>
    /// Vista principal del reporte con datos y totales.
    /// </summary>
    public async Task OnGetAsync()
    {
        try
        {
            Grupos = await _catalogService.ObtenerGruposAsync();
            Proveedores = await _catalogService.ObtenerProveedoresAsync();
            Piezas = await _reporteService.ObtenerPiezasParaReporteAsync(Buscar, IdGrupo, Proveedor);
            Totales = await _reporteService.ObtenerTotalesAsync(Buscar, IdGrupo, Proveedor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar reporte de piezas");
            TempData["Error"] = $"Error al generar reporte: {ex.Message}";
        }
    }

    /// <summary>
    /// Exportar a Excel (.xlsx) con los filtros aplicados.
    /// </summary>
    public async Task<IActionResult> OnGetExcelAsync()
    {
        try
        {
            var bytes = await _reporteService.ExportarExcelAsync(Buscar, IdGrupo, Proveedor);
            var fileName = $"Piezas_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al exportar Excel");
            TempData["Error"] = $"Error al exportar: {ex.Message}";
            return RedirectToPage();
        }
    }

    /// <summary>
    /// Exportar a PDF con los filtros aplicados.
    /// </summary>
    public async Task<IActionResult> OnGetPdfAsync()
    {
        try
        {
            var bytes = await _reporteService.ExportarPdfAsync(Buscar, IdGrupo, Proveedor);
            var fileName = $"Piezas_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
            return File(bytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al exportar PDF");
            TempData["Error"] = $"Error al exportar PDF: {ex.Message}";
            return RedirectToPage();
        }
    }
}
