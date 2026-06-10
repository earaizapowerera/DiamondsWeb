using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Inventario.InventarioFisico;

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
    public List<PiezaSobrante> Sobrantes { get; set; } = new();
    public List<PiezaFaltante> Faltantes { get; set; } = new();
    public InventarioStats Stats { get; set; } = new();
    public string? ErrorMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Tab { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Stats = await _service.ObtenerEstadisticasAsync();
            var tab = Tab ?? "registros";

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

    // AJAX: Registrar escaneo
    public async Task<IActionResult> OnPostEscanearAsync([FromBody] EscanearRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.CodigoBarras))
            return new JsonResult(new EscaneoResult { Success = false, Message = "Codigo vacio" });

        var userId = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid : 1;
        var result = await _service.RegistrarEscaneoAsync(request.CodigoBarras, userId);
        return new JsonResult(result);
    }

    // AJAX: Guardar datos de sobrante
    public async Task<IActionResult> OnPostSobranteAsync([FromBody] SobranteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.CodigoBarras))
            return new JsonResult(new { success = false });

        var userId = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid : 1;
        var ok = await _service.RegistrarSobranteAsync(
            request.CodigoBarras, request.Descripcion, request.Precio, userId);

        return new JsonResult(new { success = ok, message = ok ? "Sobrante registrada" : "Error al registrar" });
    }

    // POST: Iniciar inventario
    public async Task<IActionResult> OnPostIniciarAsync()
    {
        try
        {
            var userId = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid : 1;
            var msg = await _service.IniciarInventarioAsync(userId);
            TempData["Success"] = msg;
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error: {ex.Message}";
        }
        return RedirectToPage();
    }

    // AJAX: Eliminar registro
    public async Task<IActionResult> OnPostEliminarAsync([FromBody] EliminarRequest request)
    {
        var ok = await _service.EliminarRegistroAsync(request.Id);
        return new JsonResult(new { success = ok });
    }

    // GET: Exportar Excel (.xlsx)
    public async Task<IActionResult> OnGetExportarAsync()
    {
        var bytes = await _service.ExportarExcelAsync();
        var fileName = $"InventarioFisico_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    // AJAX: Stats actualizadas
    public async Task<IActionResult> OnGetStatsAsync()
    {
        return new JsonResult(await _service.ObtenerEstadisticasAsync());
    }

    // AJAX: Registros para refrescar tabla
    public async Task<IActionResult> OnGetRegistrosAsync()
    {
        var registros = await _service.ObtenerRegistrosAsync(Buscar);
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
