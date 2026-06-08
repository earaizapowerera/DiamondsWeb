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

    public IndexModel(AmlService amlService, AmlConfig config)
    {
        _amlService = amlService;
        _config = config;
    }

    public List<ClienteAmlResumen> Clientes { get; set; } = new();
    public AmlDashboardStats Stats { get; set; } = new();
    public AmlConfig Config => _config;

    [BindProperty(SupportsGet = true)]
    public DateTime? FechaDesde { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? FechaHasta { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? BuscarCliente { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? NivelAlerta { get; set; }

    [BindProperty(SupportsGet = true)]
    public string AgrupadorCliente { get; set; } = "NombreCliente";

    public async Task OnGetAsync()
    {
        // Default: últimos 6 meses
        FechaHasta ??= DateTime.UtcNow;
        FechaDesde ??= FechaHasta.Value.AddMonths(-_config.MesesAcumulacion);

        var filtros = new AmlFiltros
        {
            FechaDesde = FechaDesde,
            FechaHasta = FechaHasta,
            BuscarCliente = BuscarCliente,
            NivelAlerta = NivelAlerta,
            AgrupadorCliente = AgrupadorCliente
        };

        Clientes = await _amlService.ObtenerResumenClientesAsync(filtros);
        Stats = await _amlService.ObtenerEstadisticasAsync(FechaDesde.Value, FechaHasta.Value);
    }
}
