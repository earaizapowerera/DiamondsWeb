using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Catalogos.Grupos;

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

    public List<Grupo> Grupos { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    [BindProperty]
    public string NuevoNombre { get; set; } = "";

    [BindProperty]
    public int? EditId { get; set; }

    [BindProperty]
    public string? EditNombre { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Grupos = await _catalogService.ObtenerGruposAsync();
            if (!string.IsNullOrWhiteSpace(Buscar))
                Grupos = Grupos.Where(g => g.Grupo1.Contains(Buscar, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar grupos");
            TempData["Error"] = $"Error al cargar grupos: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NuevoNombre))
            {
                TempData["Error"] = "El nombre del grupo es requerido.";
                return RedirectToPage();
            }

            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid : 1;
            await _catalogService.CrearGrupoAsync(NuevoNombre.Trim(), idUsuario);
            TempData["Success"] = "Grupo creado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear grupo");
            TempData["Error"] = $"Error al crear grupo: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditAsync()
    {
        try
        {
            if (EditId == null || string.IsNullOrWhiteSpace(EditNombre))
            {
                TempData["Error"] = "Datos incompletos para editar.";
                return RedirectToPage();
            }

            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid : 1;
            await _catalogService.ActualizarGrupoAsync(EditId.Value, EditNombre.Trim(), idUsuario);
            TempData["Success"] = "Grupo actualizado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar grupo");
            TempData["Error"] = $"Error al actualizar grupo: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            await _catalogService.EliminarGrupoAsync(id);
            TempData["Success"] = "Grupo eliminado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar grupo {Id}", id);
            TempData["Error"] = $"Error al eliminar grupo: {ex.Message}";
        }

        return RedirectToPage();
    }
}
