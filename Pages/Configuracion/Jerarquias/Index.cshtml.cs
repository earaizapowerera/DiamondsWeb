using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Configuracion.Jerarquias;

[Authorize]
public class IndexModel : PageModel
{
    private readonly CatalogService _catalogService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(CatalogService catalogService, ILogger<IndexModel> logger)
    {
        _catalogService = catalogService;
        _logger = logger;
    }

    public List<TablaJerarquia> Tablas { get; set; } = new();
    public List<Jerarquia> Columnas { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? TablaSeleccionada { get; set; }

    // ── Tabla fields ──
    [BindProperty] public string NuevaTabla { get; set; } = "";

    // ── Columna fields ──
    [BindProperty] public int NuevaColumnaTablaId { get; set; }
    [BindProperty] public string NuevaColumna { get; set; } = "";
    [BindProperty] public int NuevaColumnaOrden { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Tablas = await _catalogService.ObtenerTablasJerarquiasAsync();

            if (TablaSeleccionada.HasValue)
                Columnas = await _catalogService.ObtenerJerarquiasAsync(TablaSeleccionada.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar jerarquias");
            TempData["Error"] = $"Error al cargar jerarquias: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostCreateTablaAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NuevaTabla))
            {
                TempData["Error"] = "La descripcion de la tabla es requerida.";
                return RedirectToPage();
            }

            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid : 1;
            await _catalogService.CrearTablaJerarquiaAsync(NuevaTabla.Trim(), idUsuario);
            TempData["Success"] = "Tabla creada exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear tabla");
            TempData["Error"] = $"Error al crear tabla: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteTablaAsync(int id)
    {
        try
        {
            await _catalogService.EliminarTablaJerarquiaAsync(id);
            TempData["Success"] = "Tabla y sus columnas eliminadas exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar tabla {Id}", id);
            TempData["Error"] = $"Error al eliminar tabla: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCreateColumnaAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NuevaColumna))
            {
                TempData["Error"] = "El nombre de la columna es requerido.";
                return RedirectToPage(new { TablaSeleccionada = NuevaColumnaTablaId });
            }

            await _catalogService.CrearJerarquiaAsync(NuevaColumnaTablaId, NuevaColumna.Trim(), NuevaColumnaOrden);
            TempData["Success"] = "Columna creada exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear columna");
            TempData["Error"] = $"Error al crear columna: {ex.Message}";
        }

        return RedirectToPage(new { TablaSeleccionada = NuevaColumnaTablaId });
    }

    public async Task<IActionResult> OnPostDeleteColumnaAsync(int id, int tablaId)
    {
        try
        {
            await _catalogService.EliminarJerarquiaAsync(id);
            TempData["Success"] = "Columna eliminada exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar columna {Id}", id);
            TempData["Error"] = $"Error al eliminar columna: {ex.Message}";
        }

        return RedirectToPage(new { TablaSeleccionada = tablaId });
    }
}
