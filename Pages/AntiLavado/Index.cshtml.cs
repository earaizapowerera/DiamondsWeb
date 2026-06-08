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
    private readonly SppldXmlService _sppldService;
    private readonly SppldConfig _sppldConfig;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(AmlService amlService, AmlConfig config,
        SppldXmlService sppldService, SppldConfig sppldConfig,
        ILogger<IndexModel> logger)
    {
        _amlService = amlService;
        _config = config;
        _sppldService = sppldService;
        _sppldConfig = sppldConfig;
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

    /// <summary>
    /// Genera y descarga el XML para SPPLD (Anexo 6)
    /// </summary>
    public async Task<IActionResult> OnPostGenerarXmlAsync(int mes, int anio)
    {
        var clientes = await _amlService.ObtenerClientesParaReporteAsync(
            mes, anio, null, null);

        if (!clientes.Any())
            return RedirectToPage(new { Mes = mes, Anio = anio });

        // Obtener operaciones detalladas por cada cliente
        var operacionesPorCliente = new Dictionary<string, List<NotaDetalle>>();
        foreach (var cliente in clientes)
        {
            var notas = await _amlService.ObtenerNotasClienteAsync(
                cliente.NombreCliente, mes, anio);
            operacionesPorCliente[cliente.NombreCliente] = notas;
        }

        var xmlBytes = _sppldService.GenerarXmlAviso(
            _sppldConfig, mes, anio, clientes, operacionesPorCliente);

        var fileName = $"SPPLD_Anexo6_{anio}{mes:D2}.xml";
        return File(xmlBytes, "application/xml", fileName);
    }
}
