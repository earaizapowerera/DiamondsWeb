using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Ventas.ConsultaBajas;

[Authorize]
public class IndexModel : PageModel
{
    private readonly SalesService _salesService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(SalesService salesService, ILogger<IndexModel> logger)
    {
        _salesService = salesService;
        _logger = logger;
    }

    public List<BajaPieza> Bajas { get; set; } = new();
    public int TotalPiezas { get; set; }
    public decimal SumaPrecio { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Bajas = await _salesService.ObtenerBajasPiezasAsync(Buscar);
            TotalPiezas = Bajas.Count;
            SumaPrecio = Bajas.Sum(b => b.Precio ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar bajas de piezas");
            TempData["Error"] = $"Error al consultar bajas: {ex.Message}";
        }
    }
}
