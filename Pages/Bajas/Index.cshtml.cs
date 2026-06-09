using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Bajas;

[Authorize]
public class IndexModel : PageModel
{
    private readonly BajasService _bajasService;
    private readonly ILogger<IndexModel> _logger;
    private const int TamanioPagina = 50;

    public IndexModel(BajasService bajasService, ILogger<IndexModel> logger)
    {
        _bajasService = bajasService;
        _logger = logger;
    }

    public List<BajaPiezaItem> Piezas { get; set; } = new();
    public BajasStats Stats { get; set; } = new();
    public List<string> Grupos { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public int TotalRegistros { get; set; }
    public int TotalPaginas => (int)Math.Ceiling((double)TotalRegistros / TamanioPagina);

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FechaDesde { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FechaHasta { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Grupo { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Pagina { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public string Modo { get; set; } = "resumen";

    public async Task OnGetAsync()
    {
        if (Pagina < 1) Pagina = 1;

        DateTime? fechaDesde = null;
        DateTime? fechaHasta = null;

        if (DateTime.TryParse(FechaDesde, out var fd)) fechaDesde = fd;
        if (DateTime.TryParse(FechaHasta, out var fh)) fechaHasta = fh;

        try
        {
            // Cargar grupos para el dropdown
            Grupos = await _bajasService.ObtenerGruposAsync();

            // Obtener stats (conteo + suma)
            Stats = await _bajasService.ObtenerStatsAsync(Buscar, fechaDesde, fechaHasta, Grupo);
            TotalRegistros = await _bajasService.ContarPiezasAsync(Buscar, fechaDesde, fechaHasta, Grupo);

            // Obtener piezas paginadas
            Piezas = await _bajasService.BuscarPiezasAsync(
                Buscar, fechaDesde, fechaHasta, Grupo, Pagina, TamanioPagina);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cargando consulta de bajas");
            ErrorMessage = $"Error al consultar: {ex.Message}";
        }
    }
}
