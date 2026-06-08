using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.AntiLavado;

[Authorize]
public class DetalleModel : PageModel
{
    private readonly AmlService _amlService;
    private readonly AmlConfig _config;

    public DetalleModel(AmlService amlService, AmlConfig config)
    {
        _amlService = amlService;
        _config = config;
    }

    public List<NotaDetalle> Notas { get; set; } = new();
    public string ClienteNombre { get; set; } = string.Empty;
    public decimal TotalAcumulado { get; set; }
    public string NivelAlerta { get; set; } = "Normal";
    public AmlConfig Config => _config;

    [BindProperty(SupportsGet = true)]
    public string? Cliente { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? FechaDesde { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? FechaHasta { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Agrupador { get; set; } = "NombreCliente";

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrEmpty(Cliente))
            return RedirectToPage("Index");

        FechaHasta ??= DateTime.UtcNow;
        FechaDesde ??= FechaHasta.Value.AddMonths(-_config.MesesAcumulacion);

        ClienteNombre = Cliente;
        Notas = await _amlService.ObtenerNotasClienteAsync(
            Cliente, FechaDesde.Value, FechaHasta.Value, Agrupador);

        TotalAcumulado = Notas.Sum(n => n.Total);

        if (TotalAcumulado >= _config.MontoAvisoSAT)
            NivelAlerta = "AvisoSAT";
        else if (TotalAcumulado >= _config.MontoIdentificacion)
            NivelAlerta = "Identificacion";
        else
            NivelAlerta = "Normal";

        return Page();
    }
}
