using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Ventas.ConsultaBajasCentral;

/// <summary>
/// Consulta de Bajas Central — Piezas vendidas desde la BD central (Diamhost).
/// Migración de frmCB.frm (Consultas2.vbp).
/// En VB6 requería código de autorización temporal; en web usa permisos UserPortal.
/// </summary>
[Authorize]
public class IndexModel : PageModel
{
    private readonly BajasService _bajasService;
    private readonly ILogger<IndexModel> _logger;
    private const int TamanioPagina = 50;

    public IndexModel(
        [FromKeyedServices("central")] BajasService bajasService,
        ILogger<IndexModel> logger)
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
            Grupos = await _bajasService.ObtenerGruposAsync();
            Stats = await _bajasService.ObtenerStatsAsync(Buscar, fechaDesde, fechaHasta, Grupo);
            TotalRegistros = await _bajasService.ContarPiezasAsync(Buscar, fechaDesde, fechaHasta, Grupo);
            Piezas = await _bajasService.BuscarPiezasAsync(
                Buscar, fechaDesde, fechaHasta, Grupo, Pagina, TamanioPagina);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando bajas centrales");
            ErrorMessage = $"Error al consultar BD central: {ex.Message}";
        }
    }
}
