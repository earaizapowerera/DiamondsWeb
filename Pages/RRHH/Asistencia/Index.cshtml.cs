using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.RRHH.Asistencia;

[Authorize]
public class IndexModel : PageModel
{
    private readonly AsistenciaService _asistenciaService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(AsistenciaService asistenciaService, ILogger<IndexModel> logger)
    {
        _asistenciaService = asistenciaService;
        _logger = logger;
    }

    // --- Datos para la vista ---
    public List<AsistenciaItem> Registros { get; set; } = new();
    public List<EmpleadoItem> Empleados { get; set; } = new();

    // --- Filtros (GET) ---
    [BindProperty(SupportsGet = true)]
    public int? FiltroEmpleado { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FiltroMovimiento { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? FiltroFechaDesde { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? FiltroFechaHasta { get; set; }

    // --- Campos para nuevo registro ---
    [BindProperty]
    public int NuevoIdEmpleado { get; set; }

    [BindProperty]
    public string NuevoMovimiento { get; set; } = string.Empty;

    // --- Campos para edicion ---
    [BindProperty]
    public int EditId { get; set; }

    [BindProperty]
    public int EditIdEmpleado { get; set; }

    [BindProperty]
    public string EditMovimiento { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        await CargarDatosAsync();
    }

    /// <summary>
    /// Registra una nueva entrada o salida de empleado.
    /// Despues de guardar, resetea automaticamente para modo scan rapido.
    /// </summary>
    public async Task<IActionResult> OnPostRegistrarAsync()
    {
        try
        {
            if (NuevoIdEmpleado <= 0)
            {
                TempData["Error"] = "Debe seleccionar un empleado.";
                return RedirectToPage();
            }

            if (NuevoMovimiento != "E" && NuevoMovimiento != "S")
            {
                TempData["Error"] = "Debe seleccionar Entrada o Salida.";
                return RedirectToPage();
            }

            await _asistenciaService.CreateAsync(NuevoIdEmpleado, NuevoMovimiento);
            TempData["Success"] = "Asistencia registrada exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar asistencia");
            TempData["Error"] = $"Error al registrar asistencia: {ex.Message}";
        }

        return RedirectToPage();
    }

    /// <summary>
    /// Actualiza un registro de asistencia existente.
    /// </summary>
    public async Task<IActionResult> OnPostEditarAsync()
    {
        try
        {
            if (EditId <= 0)
            {
                TempData["Error"] = "Registro no valido para edicion.";
                return RedirectToPage();
            }

            if (EditIdEmpleado <= 0)
            {
                TempData["Error"] = "Debe seleccionar un empleado.";
                return RedirectToPage();
            }

            if (EditMovimiento != "E" && EditMovimiento != "S")
            {
                TempData["Error"] = "Debe seleccionar Entrada o Salida.";
                return RedirectToPage();
            }

            var updated = await _asistenciaService.UpdateAsync(EditId, EditIdEmpleado, EditMovimiento);
            TempData["Success"] = updated
                ? "Registro actualizado exitosamente."
                : "No se encontro el registro a actualizar.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al editar asistencia {Id}", EditId);
            TempData["Error"] = $"Error al editar asistencia: {ex.Message}";
        }

        return RedirectToPage();
    }

    /// <summary>
    /// Elimina un registro de asistencia.
    /// </summary>
    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        try
        {
            var deleted = await _asistenciaService.DeleteAsync(id);
            TempData["Success"] = deleted
                ? "Registro eliminado exitosamente."
                : "No se encontro el registro a eliminar.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar asistencia {Id}", id);
            TempData["Error"] = $"Error al eliminar asistencia: {ex.Message}";
        }

        return RedirectToPage();
    }

    private async Task CargarDatosAsync()
    {
        try
        {
            Empleados = await _asistenciaService.GetEmpleadosAsync();
            Registros = await _asistenciaService.GetAllAsync(
                FiltroEmpleado,
                FiltroMovimiento,
                FiltroFechaDesde,
                FiltroFechaHasta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar datos de asistencia");
            TempData["Error"] = $"Error al cargar datos: {ex.Message}";
        }
    }
}
