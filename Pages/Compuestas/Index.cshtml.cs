using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Compuestas;

[Authorize]
public class IndexModel : PageModel
{
    private readonly CompuestaService _service;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(CompuestaService service, ILogger<IndexModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    public List<CompuestaResumen> Compuestas { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Compuestas = await _service.ObtenerCompuestasAsync(Buscar);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cargando compuestas");
            ErrorMessage = $"Error al consultar datos: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostEliminarAsync(string cb)
    {
        try
        {
            await _service.EliminarCompuestaAsync(cb, idUsuario: 1);
            SuccessMessage = $"Compuesta {cb} eliminada correctamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando compuesta {CB}", cb);
            ErrorMessage = $"Error al eliminar: {ex.Message}";
        }

        Compuestas = await _service.ObtenerCompuestasAsync(Buscar);
        return Page();
    }
}
