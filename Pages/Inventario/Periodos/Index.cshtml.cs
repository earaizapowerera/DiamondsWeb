using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Inventario.Periodos;

[Authorize]
public class IndexModel : PageModel
{
    private readonly PeriodosInventarioService _service;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(PeriodosInventarioService service, ILogger<IndexModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    public List<PeriodoInventarioDetalle> Periodos { get; set; } = new();
    public int TotalPeriodos { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    [TempData]
    public string? MensajeExito { get; set; }

    [TempData]
    public string? MensajeError { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Periodos = await _service.ListarAsync(Buscar);
            TotalPeriodos = await _service.ContarAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar períodos de inventario");
            MensajeError = $"Error al cargar períodos: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostGuardarAsync(
        int idPeriodo, DateTime periodoDesde, DateTime? periodoHasta)
    {
        if (periodoHasta.HasValue && periodoHasta < periodoDesde)
        {
            MensajeError = "El Periodo Hasta no puede ser anterior al Periodo Desde.";
            return RedirectToPage();
        }

        try
        {
            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid
                : throw new UnauthorizedAccessException("IdUsuario claim not found");

            if (idPeriodo > 0)
            {
                await _service.ActualizarAsync(idPeriodo, periodoDesde, periodoHasta, idUsuario);
                MensajeExito = $"Período #{idPeriodo} actualizado correctamente.";
            }
            else
            {
                var nuevoId = await _service.CrearAsync(periodoDesde, periodoHasta, idUsuario);
                MensajeExito = $"Período #{nuevoId} registrado correctamente.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar período de inventario");
            MensajeError = $"Error al guardar: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEliminarAsync(int idPeriodo)
    {
        try
        {
            var periodo = await _service.ObtenerPorIdAsync(idPeriodo);
            await _service.EliminarAsync(idPeriodo);
            MensajeExito = $"Período #{idPeriodo} eliminado correctamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar período {Id}", idPeriodo);
            MensajeError = $"Error al eliminar: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnGetExportarExcelAsync()
    {
        try
        {
            var bytes = await _service.ExportarExcelAsync();
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Periodos_Inventario_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al exportar períodos a Excel");
            MensajeError = $"Error al exportar: {ex.Message}";
            return RedirectToPage();
        }
    }
}
