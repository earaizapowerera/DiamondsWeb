using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Jerarquias;

[Authorize]
public class IndexModel : PageModel
{
    private readonly JerarquiasService _service;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(JerarquiasService service, ILogger<IndexModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    public List<TablaJerarquia> Tablas { get; set; } = new();
    public TablaJerarquia? TablaSeleccionada { get; set; }
    public List<Jerarquia> Jerarquias { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? TablaId { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Tablas = await _service.ObtenerTablasAsync(Buscar);

            if (TablaId.HasValue)
            {
                TablaSeleccionada = await _service.ObtenerTablaPorIdAsync(TablaId.Value);
                if (TablaSeleccionada != null)
                    Jerarquias = await _service.ObtenerJerarquiasAsync(TablaId.Value);
            }
            else if (Tablas.Count > 0)
            {
                // Seleccionar la primera tabla por defecto
                TablaId = Tablas[0].IdTabla;
                TablaSeleccionada = Tablas[0];
                Jerarquias = await _service.ObtenerJerarquiasAsync(TablaId.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cargando jerarquías");
            ErrorMessage = $"Error al cargar datos: {ex.Message}";
        }
    }

    // ── Master CRUD ───────────────────────────────────────────

    public async Task<IActionResult> OnPostCrearTablaAsync(string descripcion)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
        {
            TempData["Error"] = "La descripcion es requerida.";
            return RedirectToPage(new { Buscar, TablaId });
        }

        try
        {
            var id = await _service.CrearTablaAsync(descripcion.Trim());
            TempData["Success"] = $"Tabla '{descripcion}' creada exitosamente.";
            return RedirectToPage(new { TablaId = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear tabla");
            TempData["Error"] = $"Error al crear: {ex.Message}";
            return RedirectToPage(new { Buscar, TablaId });
        }
    }

    public async Task<IActionResult> OnPostEditarTablaAsync(int idTabla, string descripcion)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
        {
            TempData["Error"] = "La descripcion es requerida.";
            return RedirectToPage(new { Buscar, TablaId = idTabla });
        }

        try
        {
            await _service.ActualizarTablaAsync(idTabla, descripcion.Trim());
            TempData["Success"] = "Tabla actualizada.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al editar tabla {Id}", idTabla);
            TempData["Error"] = $"Error al editar: {ex.Message}";
        }

        return RedirectToPage(new { Buscar, TablaId = idTabla });
    }

    public async Task<IActionResult> OnPostEliminarTablaAsync(int idTabla)
    {
        try
        {
            await _service.EliminarTablaAsync(idTabla);
            TempData["Success"] = "Tabla y sus columnas eliminadas.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar tabla {Id}", idTabla);
            TempData["Error"] = $"Error al eliminar: {ex.Message}";
        }

        return RedirectToPage(new { Buscar });
    }

    // ── Detail CRUD ───────────────────────────────────────────

    public async Task<IActionResult> OnPostCrearColumnaAsync(int idTabla, string columna)
    {
        if (string.IsNullOrWhiteSpace(columna))
        {
            TempData["Error"] = "Seleccione una columna.";
            return RedirectToPage(new { Buscar, TablaId = idTabla });
        }

        try
        {
            await _service.CrearJerarquiaAsync(idTabla, columna.Trim());
            TempData["Success"] = $"Columna '{columna}' agregada.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear columna en tabla {Id}", idTabla);
            TempData["Error"] = $"Error al agregar columna: {ex.Message}";
        }

        return RedirectToPage(new { Buscar, TablaId = idTabla });
    }

    public async Task<IActionResult> OnPostEditarColumnaAsync(int idJerarquia, int idTabla, string columna)
    {
        if (string.IsNullOrWhiteSpace(columna))
        {
            TempData["Error"] = "Seleccione una columna.";
            return RedirectToPage(new { Buscar, TablaId = idTabla });
        }

        try
        {
            await _service.ActualizarJerarquiaAsync(idJerarquia, columna.Trim());
            TempData["Success"] = "Columna actualizada.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al editar jerarquía {Id}", idJerarquia);
            TempData["Error"] = $"Error al editar columna: {ex.Message}";
        }

        return RedirectToPage(new { Buscar, TablaId = idTabla });
    }

    public async Task<IActionResult> OnPostEliminarColumnaAsync(int idJerarquia, int idTabla)
    {
        try
        {
            await _service.EliminarJerarquiaAsync(idJerarquia);
            TempData["Success"] = "Columna eliminada.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar jerarquía {Id}", idJerarquia);
            TempData["Error"] = $"Error al eliminar columna: {ex.Message}";
        }

        return RedirectToPage(new { Buscar, TablaId = idTabla });
    }
}
