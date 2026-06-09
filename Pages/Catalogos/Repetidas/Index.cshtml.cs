using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Catalogos.Repetidas;

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

    public List<CatalogoRepetida> Repetidas { get; set; } = new();
    public List<Proveedor> Proveedores { get; set; } = new();
    public List<Grupo> Grupos { get; set; } = new();
    public List<Divisor> Divisores { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    // ── Create fields ──
    [BindProperty] public string NuevoDescripcion { get; set; } = "";
    [BindProperty] public int? NuevoProveedor { get; set; }
    [BindProperty] public int? NuevoIdGrupo { get; set; }
    [BindProperty] public string? NuevoKilates { get; set; }
    [BindProperty] public decimal? NuevoPrecio { get; set; }
    [BindProperty] public int? NuevoIdDivisor { get; set; }

    // ── Edit fields ──
    [BindProperty] public string? EditCodigoBarras { get; set; }
    [BindProperty] public string? EditDescripcion { get; set; }
    [BindProperty] public int? EditProveedor { get; set; }
    [BindProperty] public int? EditIdGrupo { get; set; }
    [BindProperty] public string? EditKilates { get; set; }
    [BindProperty] public decimal? EditPrecio { get; set; }
    [BindProperty] public int? EditIdDivisor { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Repetidas = await _catalogService.ObtenerCatalogoRepetidasAsync();
            Proveedores = await _catalogService.ObtenerProveedoresAsync();
            Grupos = await _catalogService.ObtenerGruposAsync();
            Divisores = await _catalogService.ObtenerDivisoresAsync();

            if (!string.IsNullOrWhiteSpace(Buscar))
                Repetidas = Repetidas
                    .Where(r => (r.Descripcion ?? "").Contains(Buscar, StringComparison.OrdinalIgnoreCase)
                             || r.CodigoBarras.Contains(Buscar, StringComparison.OrdinalIgnoreCase))
                    .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar catalogo repetidas");
            TempData["Error"] = $"Error al cargar catalogo repetidas: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NuevoDescripcion))
            {
                TempData["Error"] = "La descripcion es requerida.";
                return RedirectToPage();
            }

            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid : 1;
            var item = new CatalogoRepetida
            {
                Descripcion = NuevoDescripcion.Trim(),
                Proveedor = NuevoProveedor,
                IdGrupo = NuevoIdGrupo,
                Kilates = NuevoKilates?.Trim(),
                Precio = NuevoPrecio,
                IdDivisor = NuevoIdDivisor,
                IdUsuario = idUsuario
            };
            await _catalogService.CrearCatalogoRepetidaAsync(item);
            TempData["Success"] = $"Repetida creada exitosamente. Codigo de barras: {item.CodigoBarras}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear repetida");
            TempData["Error"] = $"Error al crear repetida: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(EditCodigoBarras) || string.IsNullOrWhiteSpace(EditDescripcion))
            {
                TempData["Error"] = "Datos incompletos para editar.";
                return RedirectToPage();
            }

            var item = new CatalogoRepetida
            {
                CodigoBarras = EditCodigoBarras,
                Descripcion = EditDescripcion.Trim(),
                Proveedor = EditProveedor,
                IdGrupo = EditIdGrupo,
                Kilates = EditKilates?.Trim(),
                Precio = EditPrecio,
                IdDivisor = EditIdDivisor
            };
            await _catalogService.ActualizarCatalogoRepetidaAsync(item);
            TempData["Success"] = "Repetida actualizada exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar repetida");
            TempData["Error"] = $"Error al actualizar repetida: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        try
        {
            await _catalogService.EliminarCatalogoRepetidaAsync(id);
            TempData["Success"] = "Repetida eliminada exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar repetida {Id}", id);
            TempData["Error"] = $"Error al eliminar repetida: {ex.Message}";
        }

        return RedirectToPage();
    }
}
