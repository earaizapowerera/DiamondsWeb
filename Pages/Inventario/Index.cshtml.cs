using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Inventario;

[Authorize]
public class IndexModel : PageModel
{
    private readonly InventarioFisicoService _service;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(InventarioFisicoService service, ILogger<IndexModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    public List<RegistroInventario> Registros { get; set; } = new();
    public InventarioStats Stats { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Tab { get; set; }

    public List<PiezaSobrante> Sobrantes { get; set; } = new();
    public List<PiezaFaltante> Faltantes { get; set; } = new();

    public async Task OnGetAsync()
    {
        try
        {
            Stats = await _service.ObtenerEstadisticasAsync();
            var tab = Tab ?? "escaneadas";

            if (tab == "sobrantes")
                Sobrantes = await _service.ObtenerSobrantesAsync();
            else if (tab == "faltantes")
                Faltantes = await _service.ObtenerFaltantesAsync(Buscar);
            else
                Registros = await _service.ObtenerRegistrosAsync(Buscar);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cargando inventario fisico");
            ErrorMessage = $"Error al cargar datos: {ex.Message}";
        }
    }

    /// <summary>
    /// AJAX: Registrar escaneo de codigo de barras
    /// </summary>
    public async Task<IActionResult> OnPostEscanearAsync([FromBody] EscanearRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.CodigoBarras))
            return new JsonResult(new EscaneoResult { Success = false, Message = "Codigo vacio" });

        // userId=1 por defecto (sistema legacy no tiene UserPortal users mapeados)
        var userId = 1;
        var result = await _service.RegistrarEscaneoAsync(request.CodigoBarras, userId);
        return new JsonResult(result);
    }

    /// <summary>
    /// AJAX: Registrar datos de pieza sobrante
    /// </summary>
    public async Task<IActionResult> OnPostSobranteAsync([FromBody] SobranteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.CodigoBarras))
            return new JsonResult(new { success = false, message = "Codigo vacio" });

        var ok = await _service.RegistrarSobranteAsync(
            request.CodigoBarras, request.Descripcion, request.Precio, 1);

        return new JsonResult(new { success = ok, message = ok ? "Sobrante registrada" : "Error al registrar" });
    }

    /// <summary>
    /// POST: Iniciar nuevo inventario fisico
    /// </summary>
    public async Task<IActionResult> OnPostIniciarAsync()
    {
        try
        {
            var msg = await _service.IniciarInventarioAsync(1);
            TempData["SuccessMessage"] = msg;
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error: {ex.Message}";
        }
        return RedirectToPage();
    }

    /// <summary>
    /// AJAX: Eliminar registro
    /// </summary>
    public async Task<IActionResult> OnPostEliminarAsync([FromBody] EliminarRequest request)
    {
        var ok = await _service.EliminarRegistroAsync(request.Id);
        return new JsonResult(new { success = ok });
    }

    /// <summary>
    /// Exportar a Excel (CSV)
    /// </summary>
    public async Task<IActionResult> OnGetExportarAsync()
    {
        var bytes = await _service.ExportarExcelAsync();
        var fileName = $"InventarioFisico_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    /// <summary>
    /// AJAX: Obtener stats actualizadas
    /// </summary>
    public async Task<IActionResult> OnGetStatsAsync()
    {
        var stats = await _service.ObtenerEstadisticasAsync();
        return new JsonResult(stats);
    }

    /// <summary>
    /// AJAX: Obtener ultimos registros para tabla
    /// </summary>
    public async Task<IActionResult> OnGetRegistrosAsync(string? buscar)
    {
        var registros = await _service.ObtenerRegistrosAsync(buscar);
        return new JsonResult(registros);
    }
}

public class EscanearRequest
{
    public string CodigoBarras { get; set; } = string.Empty;
}

public class SobranteRequest
{
    public string CodigoBarras { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int? Precio { get; set; }
}

public class EliminarRequest
{
    public int Id { get; set; }
}
