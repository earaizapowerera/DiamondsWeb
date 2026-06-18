using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.RRHH.Equilibrio;

/// <summary>
/// Equilibrio de Comisiones — Balance entre ventas comisionables y pagos de comisión.
/// Origen VB6: frmEquilibrio.frm (RecursosHumanos.vbp).
/// </summary>
[Authorize]
public class IndexModel : PageModel
{
    private readonly EquilibrioService _service;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(EquilibrioService service, ILogger<IndexModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    private int GetTiendaId() =>
        int.TryParse(User.FindFirst("IdTienda")?.Value, out var id) ? id : 1;

    // ─── Datos para la vista ─────────────────────────────────────

    public EquilibrioResultado? Resultado { get; set; }
    public List<TiendaItem> Tiendas { get; set; } = new();
    public bool BusquedaRealizada { get; set; }

    // ─── Filtros ─────────────────────────────────────────────────

    [BindProperty(SupportsGet = true)]
    public int? IdTienda { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Anio { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Mes { get; set; }

    // ─── GET ─────────────────────────────────────────────────────

    public async Task OnGetAsync()
    {
        try
        {
            Tiendas = await _service.ObtenerTiendasAsync();

            // Defaults: tienda del usuario, mes actual
            IdTienda ??= GetTiendaId();
            Anio ??= DateTime.UtcNow.Year;
            Mes ??= DateTime.UtcNow.Month;

            var fechaDesde = new DateTime(Anio.Value, Mes.Value, 1);
            var fechaHasta = fechaDesde.AddMonths(1).AddDays(-1);

            Resultado = await _service.CalcularEquilibrioAsync(IdTienda.Value, fechaDesde, fechaHasta);
            BusquedaRealizada = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al calcular equilibrio de comisiones");
            TempData["Error"] = $"Error al calcular: {ex.Message}";
        }
    }
}
