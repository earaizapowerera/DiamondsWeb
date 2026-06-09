using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Catalogos;

[Authorize]
public class DiamantesModel : PageModel
{
    private readonly DiamantesService _service;
    private readonly ILogger<DiamantesModel> _logger;

    public DiamantesModel(DiamantesService service, ILogger<DiamantesModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    public List<Diamante> Diamantes { get; set; } = new();
    public List<string> Cortes { get; set; } = new();
    public List<string> Colores { get; set; } = new();
    public List<string> Purezas { get; set; } = new();
    public List<string> StatusList { get; set; } = new();
    public string? ErrorMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Busqueda { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Corte { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Color { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Pureza { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal? QuilatesMin { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal? QuilatesMax { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? PrecioMin { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? PrecioMax { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            // Cargar opciones de filtros en paralelo
            var cortesTask = _service.ObtenerCortesAsync();
            var coloresTask = _service.ObtenerColoresAsync();
            var purezasTask = _service.ObtenerPurezasAsync();
            var statusTask = _service.ObtenerStatusAsync();

            var filtros = new DiamanteFiltros
            {
                Busqueda = Busqueda,
                Corte = Corte,
                Color = Color,
                Pureza = Pureza,
                Status = Status,
                QuilatesMin = QuilatesMin,
                QuilatesMax = QuilatesMax,
                PrecioMin = PrecioMin,
                PrecioMax = PrecioMax
            };

            var diamantesTask = _service.ObtenerDiamantesAsync(filtros);

            await Task.WhenAll(cortesTask, coloresTask, purezasTask, statusTask, diamantesTask);

            Cortes = cortesTask.Result;
            Colores = coloresTask.Result;
            Purezas = purezasTask.Result;
            StatusList = statusTask.Result;
            Diamantes = diamantesTask.Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cargando catálogo de diamantes");
            ErrorMessage = $"Error al consultar datos: {ex.Message}";
        }
    }
}
