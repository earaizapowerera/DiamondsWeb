using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.AntiLavado;

[Authorize]
public class IndexModel : PageModel
{
    private readonly AmlService _amlService;
    private readonly AmlConfig _config;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(AmlService amlService, AmlConfig config, ILogger<IndexModel> logger)
    {
        _amlService = amlService;
        _config = config;
        _logger = logger;
    }

    public List<ClienteAmlResumen> Clientes { get; set; } = new();
    public AmlDashboardStats Stats { get; set; } = new();
    public AmlConfig Config => _config;
    public DateTime PeriodoDesde { get; set; }
    public DateTime PeriodoHasta { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Mes { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Anio { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? BuscarCliente { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? NivelAlerta { get; set; }

    public async Task OnGetAsync()
    {
        Mes ??= DateTime.UtcNow.Month;
        Anio ??= DateTime.UtcNow.Year;

        PeriodoHasta = new DateTime(Anio.Value, Mes.Value, DateTime.DaysInMonth(Anio.Value, Mes.Value));
        PeriodoDesde = PeriodoHasta.AddMonths(-5);
        PeriodoDesde = new DateTime(PeriodoDesde.Year, PeriodoDesde.Month, 1);

        try
        {
            Clientes = await _amlService.ObtenerClientesParaReporteAsync(
                Mes.Value, Anio.Value, BuscarCliente, NivelAlerta);
            Stats = await _amlService.ObtenerEstadisticasAsync(Mes.Value, Anio.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cargando datos AML para {Mes}/{Anio}", Mes, Anio);
            ErrorMessage = $"Error al consultar datos: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostMarcarReportadoAsync(
        string nombreCliente, string? rfc, string? telefonos,
        int mes, int anio, decimal totalAcumulado, int numOperaciones, string nivelAlerta)
    {
        var reportadoPor = User.Identity?.Name ?? "admin";
        await _amlService.MarcarComoReportadoAsync(
            nombreCliente, rfc, telefonos, mes, anio,
            totalAcumulado, numOperaciones, nivelAlerta, reportadoPor, null);

        return RedirectToPage(new { Mes = mes, Anio = anio });
    }
}
