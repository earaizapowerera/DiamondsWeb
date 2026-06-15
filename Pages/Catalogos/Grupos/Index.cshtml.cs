using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Catalogos.Grupos;

[Authorize]
public class IndexModel : PageModel
{
    private readonly GruposService _service;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(GruposService service, ILogger<IndexModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    public List<GrupoItem> Grupos { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    // Campos del formulario de alta/edición
    [BindProperty]
    public int? EditId { get; set; }

    [BindProperty]
    public string NombreGrupo { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        await CargarGruposAsync();

        if (TempData["Success"] is string msg)
            SuccessMessage = msg;
        if (TempData["Error"] is string err)
            ErrorMessage = err;
    }

    /// <summary>
    /// Crear nuevo grupo (botón Registrar sin EditId).
    /// </summary>
    public async Task<IActionResult> OnPostCrearAsync()
    {
        if (string.IsNullOrWhiteSpace(NombreGrupo))
        {
            TempData["Error"] = "El nombre del grupo es obligatorio.";
            return RedirectToPage(new { Buscar });
        }

        if (NombreGrupo.Trim().Length > 30)
        {
            TempData["Error"] = "El nombre del grupo no puede exceder 30 caracteres.";
            return RedirectToPage(new { Buscar });
        }

        if (await _service.ExisteNombreAsync(NombreGrupo))
        {
            TempData["Error"] = $"Ya existe un grupo con el nombre \"{NombreGrupo.Trim()}\".";
            return RedirectToPage(new { Buscar });
        }

        try
        {
            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid
                : throw new UnauthorizedAccessException("IdUsuario claim not found");
            var id = await _service.CrearAsync(NombreGrupo, idUsuario);
            TempData["Success"] = $"Grupo \"{NombreGrupo.Trim()}\" creado correctamente (Id: {id}).";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear grupo {Grupo}", NombreGrupo);
            TempData["Error"] = $"Error al crear grupo: {ex.Message}";
        }

        return RedirectToPage(new { Buscar });
    }

    /// <summary>
    /// Actualizar grupo existente.
    /// </summary>
    public async Task<IActionResult> OnPostEditarAsync()
    {
        if (!EditId.HasValue || EditId.Value <= 0)
        {
            TempData["Error"] = "Id de grupo inválido.";
            return RedirectToPage(new { Buscar });
        }

        if (string.IsNullOrWhiteSpace(NombreGrupo))
        {
            TempData["Error"] = "El nombre del grupo es obligatorio.";
            return RedirectToPage(new { Buscar });
        }

        if (NombreGrupo.Trim().Length > 30)
        {
            TempData["Error"] = "El nombre del grupo no puede exceder 30 caracteres.";
            return RedirectToPage(new { Buscar });
        }

        if (await _service.ExisteNombreAsync(NombreGrupo, EditId.Value))
        {
            TempData["Error"] = $"Ya existe otro grupo con el nombre \"{NombreGrupo.Trim()}\".";
            return RedirectToPage(new { Buscar });
        }

        try
        {
            var editUserId = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var eu) ? eu
                : throw new UnauthorizedAccessException("IdUsuario claim not found");
            var ok = await _service.ActualizarAsync(EditId.Value, NombreGrupo, editUserId);
            if (ok)
                TempData["Success"] = $"Grupo \"{NombreGrupo.Trim()}\" actualizado correctamente.";
            else
                TempData["Error"] = "Grupo no encontrado.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar grupo {Id}", EditId);
            TempData["Error"] = $"Error al actualizar grupo: {ex.Message}";
        }

        return RedirectToPage(new { Buscar });
    }

    /// <summary>
    /// Eliminar grupo.
    /// </summary>
    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        try
        {
            var grupo = await _service.ObtenerPorIdAsync(id);
            var ok = await _service.EliminarAsync(id);
            if (ok)
                TempData["Success"] = $"Grupo \"{grupo?.Grupo}\" eliminado correctamente.";
            else
                TempData["Error"] = "Grupo no encontrado.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar grupo {Id}", id);
            TempData["Error"] = $"Error al eliminar grupo: {ex.Message}";
        }

        return RedirectToPage(new { Buscar });
    }

    private async Task CargarGruposAsync()
    {
        try
        {
            Grupos = await _service.ListarAsync(Buscar);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar grupos");
            ErrorMessage = $"Error al consultar datos: {ex.Message}";
        }
    }
}
