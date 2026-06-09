using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Proveedores;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ProveedorService _service;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ProveedorService service, ILogger<IndexModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    public List<ProveedorResumen> Proveedores { get; set; } = new();
    public int TotalProveedores { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Proveedores = await _service.ListarAsync(Buscar);
            TotalProveedores = await _service.ContarProveedoresAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listando proveedores");
            ErrorMessage = $"Error al consultar proveedores: {ex.Message}";
        }

        if (TempData["Success"] is string msg)
            SuccessMessage = msg;
        if (TempData["Error"] is string err)
            ErrorMessage = err;
    }

    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        try
        {
            var eliminado = await _service.EliminarAsync(id);
            TempData["Success"] = eliminado
                ? $"Proveedor #{id} eliminado correctamente."
                : $"No se encontro proveedor #{id}.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando proveedor {Id}", id);
            TempData["Error"] = $"Error al eliminar: {ex.Message}";
        }

        return RedirectToPage(new { Buscar });
    }
}
