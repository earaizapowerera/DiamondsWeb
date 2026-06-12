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
    public Dictionary<int, List<PagoDetalle>> DesglosePageos { get; set; } = new();
    public string ClienteNombre { get; set; } = string.Empty;
    public decimal TotalAcumulado { get; set; }
    public string NivelAlerta { get; set; } = "Normal";
    public AmlConfig Config => _config;
    public DateTime PeriodoDesde { get; set; }
    public DateTime PeriodoHasta { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Cliente { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Mes { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Anio { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrEmpty(Cliente))
            return RedirectToPage("Index");

        Mes ??= DateTime.UtcNow.Month;
        Anio ??= DateTime.UtcNow.Year;

        PeriodoHasta = new DateTime(Anio.Value, Mes.Value, DateTime.DaysInMonth(Anio.Value, Mes.Value));
        PeriodoDesde = PeriodoHasta.AddMonths(-5);
        PeriodoDesde = new DateTime(PeriodoDesde.Year, PeriodoDesde.Month, 1);

        ClienteNombre = Cliente;
        Notas = await _amlService.ObtenerNotasClienteAsync(Cliente, Mes.Value, Anio.Value);
        TotalAcumulado = Notas.Sum(n => n.Total);

        var idNotas = Notas.Select(n => n.IdNota).ToList();
        DesglosePageos = await _amlService.ObtenerDesglosePageosAsync(idNotas);

        if (TotalAcumulado >= _config.MontoAvisoSAT)
            NivelAlerta = "AvisoSAT";
        else if (TotalAcumulado >= _config.MontoIdentificacion)
            NivelAlerta = "Identificacion";

        return Page();
    }
}
