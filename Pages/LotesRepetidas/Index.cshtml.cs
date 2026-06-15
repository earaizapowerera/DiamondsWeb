using DiamondsWeb.Extensions;
using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.LotesRepetidas;

[Authorize]
public class IndexModel : PageModel
{
    private readonly LotesRepetidasService _service;
    private readonly ILogger<IndexModel> _logger;

    // IdTienda del usuario autenticado (antes hardcodeado a 1 como en VB6)
    private int IdTienda => User.GetIdTienda();

    public IndexModel(LotesRepetidasService service, ILogger<IndexModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    // ─── Datos para la vista ─────────────────────────────────────

    public List<LoteRepetidaItem> Piezas { get; set; } = new();
    public List<Moneda> Monedas { get; set; } = new();
    public List<ProveedorConDefaults> Proveedores { get; set; } = new();
    public DefaultsFactorComunes? Defaults { get; set; }
    public Remision? RemisionActual { get; set; }
    public string? MensajeExito { get; set; }
    public string? MensajeError { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? IdRemision { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? IdFactura { get; set; }

    // ─── GET ─────────────────────────────────────────────────────

    public async Task OnGetAsync()
    {
        await CargarCatalogosAsync();

        if (IdRemision.HasValue)
        {
            RemisionActual = await _service.ObtenerRemisionAsync(IdRemision.Value);
            Piezas = await _service.ObtenerPiezasPorRemisionAsync(IdRemision.Value);
        }
        else if (IdFactura.HasValue)
        {
            Piezas = await _service.ObtenerPiezasPorFacturaAsync(IdFactura.Value);
        }
    }

    // ─── Crear Remisión ──────────────────────────────────────────

    public async Task<IActionResult> OnPostCrearRemisionAsync(
        int proveedor, string? numRemision, DateTime fechaRemision, bool consignacion)
    {
        try
        {
            var idRemision = await _service.CrearRemisionAsync(
                proveedor, numRemision, fechaRemision, consignacion, IdTienda);

            return RedirectToPage(new { IdRemision = idRemision });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear remisión");
            MensajeError = $"Error al crear remisión: {ex.Message}";
            await CargarCatalogosAsync();
            return Page();
        }
    }

    // ─── Agregar Pieza ───────────────────────────────────────────

    public async Task<IActionResult> OnPostAgregarPiezaAsync(CrearLoteRepetidaRequest pieza)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(pieza.CodigoBarras))
            {
                MensajeError = "El código de barras es obligatorio.";
                await CargarCatalogosAsync();
                return Page();
            }

            if (pieza.IdMoneda == 0)
            {
                MensajeError = "Debe seleccionar una moneda.";
                await CargarCatalogosAsync();
                return Page();
            }

            await _service.CrearPiezaEnLoteAsync(pieza, IdTienda);

            IdRemision = pieza.IdRemision;
            IdFactura = pieza.IdFactura;
            return RedirectToPage(new { IdRemision, IdFactura });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al agregar pieza al lote");
            MensajeError = $"Error al agregar pieza: {ex.Message}";
            IdRemision = pieza.IdRemision;
            IdFactura = pieza.IdFactura;
            await CargarCatalogosAsync();
            if (IdRemision.HasValue)
            {
                RemisionActual = await _service.ObtenerRemisionAsync(IdRemision.Value);
                Piezas = await _service.ObtenerPiezasPorRemisionAsync(IdRemision.Value);
            }
            return Page();
        }
    }

    // ─── Eliminar Pieza ──────────────────────────────────────────

    public async Task<IActionResult> OnPostEliminarPiezaAsync(
        string codigoBarras, DateTime fechaCaptura, int? idRemision, int? idFactura)
    {
        try
        {
            await _service.EliminarPiezaAsync(codigoBarras, fechaCaptura);
            return RedirectToPage(new { IdRemision = idRemision, IdFactura = idFactura });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar pieza");
            MensajeError = $"Error al eliminar pieza: {ex.Message}";
            IdRemision = idRemision;
            IdFactura = idFactura;
            await CargarCatalogosAsync();
            return Page();
        }
    }

    // ─── API endpoints (JSON) ────────────────────────────────────

    /// <summary>
    /// Busca pieza en catálogo por código de barras
    /// </summary>
    public async Task<IActionResult> OnGetBuscarCatalogoAsync(string codigo)
    {
        var pieza = await _service.BuscarCatalogoAsync(codigo);
        if (pieza == null)
            return new JsonResult(new { found = false });

        return new JsonResult(new
        {
            found = true,
            pieza.CodigoBarras,
            pieza.Descripcion,
            pieza.Precio,
            pieza.Proveedor,
            pieza.Kilates,
            pieza.IdDivisor
        });
    }

    /// <summary>
    /// Obtiene tipo de cambio de cotización para una moneda
    /// </summary>
    public async Task<IActionResult> OnGetTipoCambioAsync(int idMoneda)
    {
        var tc = await _service.ObtenerTipoCambioAsync(idMoneda);
        return new JsonResult(new
        {
            tipoCambioCotizacion = tc?.TipoCambioCotizacion ?? 1m,
            tipoCambioVenta = tc?.TipoCambioVenta
        });
    }

    /// <summary>
    /// Obtiene defaults del proveedor
    /// </summary>
    public async Task<IActionResult> OnGetProveedorDefaultsAsync(int idProveedor)
    {
        var prov = await _service.ObtenerProveedorAsync(idProveedor);
        if (prov == null)
            return new JsonResult(new { found = false });

        var defaults = await _service.ObtenerDefaultsFactorAsync();
        var rangosUtilidad = await _service.ObtenerRangosUtilidadExtraAsync();

        return new JsonResult(new
        {
            found = true,
            prov.IdMoneda,
            prov.Moneda,
            prov.UtilidadExtra,
            prov.CaracteristicaDefault,
            prov.DefaultUtilidadOro,
            prov.DefaultUtilidadGemas,
            prov.DefaultUtilidadReloj,
            prov.DefaultUtilidadExtra,
            defaultImpuesto = defaults?.DefaultImpuesto ?? 1m,
            defaultDivisor = defaults?.DefaultDivisor ?? 1m,
            rangosUtilidadExtra = rangosUtilidad.Select(r => new
            {
                desde = r.PrecioGramoDesde,
                hasta = r.PrecioGramoHasta,
                utilidad = r.DefaultUtilidadExtra
            })
        });
    }

    /// <summary>
    /// Busca remisiones para el selector
    /// </summary>
    public async Task<IActionResult> OnGetBuscarRemisionesAsync(string? filtro)
    {
        var remisiones = await _service.BuscarRemisionesAsync(filtro);
        return new JsonResult(remisiones.Select(r => new
        {
            r.IdRemision,
            r.NombreProveedor,
            r.NumRemision,
            fechaRemision = r.FechaRemision?.ToString("dd/MM/yyyy"),
            r.Consignacion
        }));
    }

    /// <summary>
    /// Obtiene razones sociales de un proveedor
    /// </summary>
    public async Task<IActionResult> OnGetRazonesSocialesAsync(int idProveedor)
    {
        var razones = await _service.ObtenerRazonesSocialesAsync(idProveedor);
        return new JsonResult(razones);
    }

    // ─── Helpers ─────────────────────────────────────────────────

    private async Task CargarCatalogosAsync()
    {
        Monedas = await _service.ObtenerMonedasAsync();
        Proveedores = await _service.BuscarProveedoresAsync();
        Defaults = await _service.ObtenerDefaultsFactorAsync();
    }
}
