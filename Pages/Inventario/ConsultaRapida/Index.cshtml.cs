using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Inventario.ConsultaRapida;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ConsultaRapidaService _consultaRapidaService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ConsultaRapidaService consultaRapidaService, ILogger<IndexModel> logger)
    {
        _consultaRapidaService = consultaRapidaService;
        _logger = logger;
    }

    public ConsultaRapidaResultado? Resultado { get; set; }

    [Microsoft.AspNetCore.Mvc.BindProperty(SupportsGet = true)]
    public string? CodigoBarras { get; set; }

    public bool BusquedaRealizada { get; set; }

    public async Task OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(CodigoBarras))
            return;

        BusquedaRealizada = true;

        try
        {
            Resultado = await _consultaRapidaService.BuscarPorCodigoBarrasAsync(CodigoBarras.Trim());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar pieza con CB={CodigoBarras}", CodigoBarras);
            TempData["Error"] = $"Error al buscar: {ex.Message}";
        }
    }
}
