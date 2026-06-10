using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Catalogos.TablasJerarquias;

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
    public List<Jerarquia> Jerarquias { get; set; } = new();
    public TablaJerarquia? TablaSeleccionada { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? SelId { get; set; }

    [BindProperty]
    public string NuevaDescripcion { get; set; } = "";

    [BindProperty]
    public int? EditTablaId { get; set; }

    [BindProperty]
    public string? EditDescripcion { get; set; }

    [BindProperty]
    public string NuevaColumna { get; set; } = "";

    [BindProperty]
    public int? EditJerarquiaId { get; set; }

    [BindProperty]
    public string? EditColumna { get; set; }

    public async Task OnGetAsync()
    {
        await CargarTablas();
        if (SelId.HasValue)
        {
            TablaSeleccionada = Tablas.FirstOrDefault(t => t.IdTabla == SelId.Value);
            if (TablaSeleccionada != null)
                Jerarquias = await _catalogService.ObtenerJerarquiasAsync(SelId.Value);
        }
    }

    public async Task<IActionResult> OnPostCreateTablaAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NuevaDescripcion))
            {
                TempData["Error"] = "La descripcion es requerida.";
                return RedirectToPage(new { SelId });
            }
            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid : 1;
            var newId = await _catalogService.CrearTablaJerarquiaAsync(NuevaDescripcion.Trim(), idUsuario);
            TempData["Success"] = "Tabla creada exitosamente.";
            return RedirectToPage(new { SelId = newId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear tabla de jerarquia");
            TempData["Error"] = $"Error al crear tabla: {ex.Message}";
            return RedirectToPage(new { SelId });
        }
    }

    public async Task<IActionResult> OnPostEditTablaAsync()
    {
        try
        {
            if (EditTablaId == null || string.IsNullOrWhiteSpace(EditDescripcion))
            {
                TempData["Error"] = "Datos incompletos para editar.";
                return RedirectToPage(new { SelId });
            }
            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid : 1;
            await _catalogService.ActualizarTablaJerarquiaAsync(EditTablaId.Value, EditDescripcion.Trim(), idUsuario);
            TempData["Success"] = "Tabla actualizada exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar tabla de jerarquia");
            TempData["Error"] = $"Error al actualizar tabla: {ex.Message}";
        }
        return RedirectToPage(new { SelId = EditTablaId ?? SelId });
    }

    public async Task<IActionResult> OnPostDeleteTablaAsync(int id)
    {
        try
        {
            await _catalogService.EliminarTablaJerarquiaAsync(id);
            TempData["Success"] = "Tabla y sus columnas eliminadas exitosamente.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar tabla {Id}", id);
            TempData["Error"] = $"Error al eliminar tabla: {ex.Message}";
            return RedirectToPage(new { SelId = id });
        }
    }

    public async Task<IActionResult> OnPostCreateJerarquiaAsync()
    {
        try
        {
            if (!SelId.HasValue || string.IsNullOrWhiteSpace(NuevaColumna))
            {
                TempData["Error"] = "Seleccione una tabla e ingrese el nombre de la columna.";
                return RedirectToPage(new { SelId });
            }
            await _catalogService.CrearJerarquiaAsync(SelId.Value, NuevaColumna.Trim(), 0);
            TempData["Success"] = "Columna registrada exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear jerarquia");
            TempData["Error"] = $"Error al crear columna: {ex.Message}";
        }
        return RedirectToPage(new { SelId });
    }

    public async Task<IActionResult> OnPostEditJerarquiaAsync()
    {
        try
        {
            if (EditJerarquiaId == null || string.IsNullOrWhiteSpace(EditColumna))
            {
                TempData["Error"] = "Datos incompletos para editar columna.";
                return RedirectToPage(new { SelId });
            }
            await _catalogService.ActualizarJerarquiaAsync(EditJerarquiaId.Value, EditColumna.Trim(), 0);
            TempData["Success"] = "Columna actualizada exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar jerarquia");
            TempData["Error"] = $"Error al actualizar columna: {ex.Message}";
        }
        return RedirectToPage(new { SelId });
    }

    public async Task<IActionResult> OnPostDeleteJerarquiaAsync(int id)
    {
        try
        {
            await _catalogService.EliminarJerarquiaAsync(id);
            TempData["Success"] = "Columna eliminada exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar jerarquia {Id}", id);
            TempData["Error"] = $"Error al eliminar columna: {ex.Message}";
        }
        return RedirectToPage(new { SelId });
    }

    private async Task CargarTablas()
    {
        try
        {
            Tablas = await _catalogService.ObtenerTablasJerarquiasAsync();
            if (!string.IsNullOrWhiteSpace(Buscar))
                Tablas = Tablas.Where(t => t.Descripcion.Contains(Buscar, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar tablas de jerarquias");
            TempData["Error"] = $"Error al cargar tablas: {ex.Message}";
        }
    }
}
