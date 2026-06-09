using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Ventas.ConsultaNotas;

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

    public List<NotaVenta> Notas { get; set; } = new();
    public List<PiezaNota> PiezasDetalle { get; set; } = new();
    public List<PagoNota> PagosDetalle { get; set; } = new();
    public string? NotaSeleccionada { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? Desde { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? Hasta { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? NombreCliente { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? CodigoBarras { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? IdNotaDetalle { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            if (Desde.HasValue || Hasta.HasValue || !string.IsNullOrWhiteSpace(NombreCliente) || !string.IsNullOrWhiteSpace(CodigoBarras))
            {
                Notas = await _salesService.ObtenerNotasAsync(Desde, Hasta, NombreCliente, CodigoBarras);
            }

            if (!string.IsNullOrWhiteSpace(IdNotaDetalle))
            {
                NotaSeleccionada = IdNotaDetalle;
                PiezasDetalle = await _salesService.ObtenerPiezasNotaAsync(IdNotaDetalle);
                PagosDetalle = await _salesService.ObtenerPagosNotaAsync(IdNotaDetalle);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar notas");
            TempData["Error"] = $"Error al consultar notas: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostCancelarAsync(string idNota)
    {
        try
        {
            var resultado = await _salesService.CancelarNotaAsync(idNota);
            TempData["Success"] = resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cancelar nota {IdNota}", idNota);
            TempData["Error"] = $"Error al cancelar nota: {ex.Message}";
        }

        return RedirectToPage();
    }
}
