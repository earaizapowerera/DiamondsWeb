using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;

namespace DiamondsWeb.Pages.Inventario.Periodos;

[Authorize]
public class IndexModel : PageModel
{
    private readonly PeriodosService _service;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(PeriodosService service, ILogger<IndexModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    public List<PeriodoItem> Periodos { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    // Campos del formulario de alta/edición
    [BindProperty]
    public int? EditId { get; set; }

    [BindProperty]
    public DateTime? PeriodoDesde { get; set; }

    [BindProperty]
    public DateTime? PeriodoHasta { get; set; }

    public async Task OnGetAsync()
    {
        await CargarPeriodosAsync();

        if (TempData["Success"] is string msg)
            SuccessMessage = msg;
        if (TempData["Error"] is string err)
            ErrorMessage = err;
    }

    /// <summary>
    /// Crear nuevo período. Ejecuta sp_mandarafaltantes antes del insert (VB6).
    /// </summary>
    public async Task<IActionResult> OnPostCrearAsync()
    {
        if (!PeriodoDesde.HasValue)
        {
            TempData["Error"] = "La fecha 'Período Desde' es obligatoria.";
            return RedirectToPage(new { Buscar });
        }

        if (PeriodoHasta.HasValue && PeriodoHasta.Value < PeriodoDesde.Value)
        {
            TempData["Error"] = "'Período Hasta' no puede ser anterior a 'Período Desde'.";
            return RedirectToPage(new { Buscar });
        }

        try
        {
            var idUsuario = ObtenerIdUsuario();
            var id = await _service.CrearAsync(PeriodoDesde.Value, PeriodoHasta, idUsuario);
            TempData["Success"] = $"Período #{id} creado correctamente (Desde: {PeriodoDesde.Value:dd/MM/yyyy}).";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear período");
            TempData["Error"] = $"Error al crear período: {ex.Message}";
        }

        return RedirectToPage(new { Buscar });
    }

    /// <summary>
    /// Actualizar período existente.
    /// </summary>
    public async Task<IActionResult> OnPostEditarAsync()
    {
        if (!EditId.HasValue || EditId.Value <= 0)
        {
            TempData["Error"] = "Id de período inválido.";
            return RedirectToPage(new { Buscar });
        }

        if (!PeriodoDesde.HasValue)
        {
            TempData["Error"] = "La fecha 'Período Desde' es obligatoria.";
            return RedirectToPage(new { Buscar });
        }

        if (PeriodoHasta.HasValue && PeriodoHasta.Value < PeriodoDesde.Value)
        {
            TempData["Error"] = "'Período Hasta' no puede ser anterior a 'Período Desde'.";
            return RedirectToPage(new { Buscar });
        }

        try
        {
            var idUsuario = ObtenerIdUsuario();
            var ok = await _service.ActualizarAsync(EditId.Value, PeriodoDesde.Value, PeriodoHasta, idUsuario);
            if (ok)
                TempData["Success"] = $"Período #{EditId.Value} actualizado correctamente.";
            else
                TempData["Error"] = "Período no encontrado.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar período {Id}", EditId);
            TempData["Error"] = $"Error al actualizar período: {ex.Message}";
        }

        return RedirectToPage(new { Buscar });
    }

    /// <summary>
    /// Eliminar período.
    /// </summary>
    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        try
        {
            var periodo = await _service.ObtenerPorIdAsync(id);
            var ok = await _service.EliminarAsync(id);
            if (ok)
                TempData["Success"] = $"Período #{id} eliminado correctamente.";
            else
                TempData["Error"] = "Período no encontrado.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar período {Id}", id);
            TempData["Error"] = $"Error al eliminar período: {ex.Message}";
        }

        return RedirectToPage(new { Buscar });
    }

    /// <summary>
    /// Exportar períodos a CSV (descarga directa).
    /// </summary>
    public async Task<IActionResult> OnGetExportarAsync()
    {
        try
        {
            var periodos = await _service.ExportarAsync();

            var sb = new StringBuilder();
            sb.AppendLine("IdPeriodo,PeriodoDesde,PeriodoHasta,FechaCaptura,FechaUltEdicion,Usuario");
            foreach (var p in periodos)
            {
                sb.AppendLine(string.Join(",",
                    p.IdPeriodo,
                    p.PeriodoDesde?.ToString("dd/MM/yyyy") ?? "",
                    p.PeriodoHasta?.ToString("dd/MM/yyyy") ?? "",
                    p.FechaCaptura?.ToString("dd/MM/yyyy HH:mm:ss") ?? "",
                    p.FechaUltEdicion?.ToString("dd/MM/yyyy HH:mm:ss") ?? "",
                    $"\"{p.NombreUsuario ?? ""}\""
                ));
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            var fileName = $"Periodos_Inventario_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            return File(bytes, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al exportar períodos");
            TempData["Error"] = $"Error al exportar: {ex.Message}";
            return RedirectToPage();
        }
    }

    private async Task CargarPeriodosAsync()
    {
        try
        {
            Periodos = await _service.ListarAsync(Buscar);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar períodos");
            ErrorMessage = $"Error al consultar datos: {ex.Message}";
        }
    }

    private int ObtenerIdUsuario()
    {
        return int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid)
            ? uid
            : throw new UnauthorizedAccessException("IdUsuario claim not found");
    }
}
