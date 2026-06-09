using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Procesos.ActualizacionPieza;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ActualizacionService _svc;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ActualizacionService svc, ILogger<IndexModel> logger)
    {
        _svc = svc;
        _logger = logger;
    }

    // ── Datos para la vista ──
    public PiezaActualizacion? Pieza { get; set; }
    public FacturaBusqueda? FacturaActual { get; set; }
    public List<MonedaCatalogo> Monedas { get; set; } = new();

    // ── Búsqueda ──
    [BindProperty(SupportsGet = true)]
    public string? CodigoBarras { get; set; }

    // ── Guardado ──
    [BindProperty] public string? GuardarCB { get; set; }
    [BindProperty] public int? GuardarIdFactura { get; set; }
    [BindProperty] public decimal? GuardarCBPieza { get; set; }
    [BindProperty] public decimal? GuardarCNPieza { get; set; }
    [BindProperty] public decimal? GuardarDescPieza { get; set; }
    [BindProperty] public int? GuardarIdMoneda { get; set; }
    [BindProperty] public decimal? GuardarTC { get; set; }
    [BindProperty] public decimal? GuardarCBFactura { get; set; }
    [BindProperty] public decimal? GuardarCNFactura { get; set; }

    // ── Alta de factura ──
    [BindProperty] public string? NuevaFolio { get; set; }
    [BindProperty] public int? NuevaProveedor { get; set; }
    [BindProperty] public int? NuevaRazonSocial { get; set; }
    [BindProperty] public DateTime? NuevaFecha { get; set; }

    // ═══════════════════════════════════════════
    // GET principal
    // ═══════════════════════════════════════════
    public async Task OnGetAsync()
    {
        try
        {
            Monedas = await _svc.ObtenerMonedasAsync();

            if (!string.IsNullOrWhiteSpace(CodigoBarras))
            {
                var piezas = await _svc.BuscarPiezasAsync(CodigoBarras.Trim());
                Pieza = piezas.FirstOrDefault(p =>
                    p.CodigoBarras == CodigoBarras.Trim());
                Pieza ??= piezas.FirstOrDefault();

                if (Pieza == null)
                {
                    TempData["Error"] = $"Pieza '{CodigoBarras}' no encontrada.";
                }
                else if (Pieza.IdFactura.HasValue)
                {
                    FacturaActual = await _svc.ObtenerFacturaPorIdAsync(Pieza.IdFactura.Value);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error buscando pieza {CB}", CodigoBarras);
            TempData["Error"] = $"Error: {ex.Message}";
        }
    }

    // ═══════════════════════════════════════════
    // AJAX: Buscar factura por folio + proveedor
    // ═══════════════════════════════════════════
    public async Task<IActionResult> OnGetBuscarFacturaAsync(string folio, int proveedor)
    {
        try
        {
            var factura = await _svc.BuscarFacturaPorFolioYProveedorAsync(folio.Trim(), proveedor);
            if (factura != null)
                return new JsonResult(new { encontrada = true, factura });

            var razones = await _svc.ObtenerRazonesSocialesPorProveedorAsync(proveedor);
            return new JsonResult(new { encontrada = false, razonesSociales = razones });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error buscando factura");
            return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }

    // ═══════════════════════════════════════════
    // AJAX: Crear factura nueva
    // ═══════════════════════════════════════════
    public async Task<IActionResult> OnPostCrearFacturaAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NuevaFolio) || !NuevaProveedor.HasValue
                || !NuevaRazonSocial.HasValue || !NuevaFecha.HasValue)
                return new JsonResult(new { error = "Campos requeridos." }) { StatusCode = 400 };

            var uid = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var u) ? u : 1;
            var id = await _svc.CrearFacturaAsync(
                NuevaFolio.Trim(), NuevaProveedor.Value,
                NuevaRazonSocial.Value, NuevaFecha.Value, uid, idTienda: 1);

            var factura = await _svc.ObtenerFacturaPorIdAsync(id);
            return new JsonResult(new { success = true, factura });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creando factura");
            return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }

    // ═══════════════════════════════════════════
    // POST: Guardar costos (replica VB6 cmdAceptar_Click)
    // ═══════════════════════════════════════════
    public async Task<IActionResult> OnPostGuardarAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(GuardarCB) || !GuardarIdFactura.HasValue)
            {
                TempData["Error"] = "Debe seleccionar pieza y factura.";
                return RedirectToPage(new { CodigoBarras = GuardarCB });
            }

            var idMoneda = GuardarIdMoneda ?? 1;
            var tc = GuardarTC ?? 1m;
            var cbPieza = GuardarCBPieza ?? 0m;
            var cnPieza = GuardarCNPieza ?? 0m;
            var cbFactura = GuardarCBFactura ?? 0m;
            var cnFactura = GuardarCNFactura ?? 0m;

            // descFactura = 100 × (1 - neto/bruto) — lógica VB6
            decimal descFactura = cbFactura > 0 ? 100m * (1m - cnFactura / cbFactura) : 0m;

            // VB6 legacy: para moneda extranjera CBPieza/CNPieza guardan valores MN
            var dto = new ActualizarCostoPiezaDto
            {
                CodigoBarras = GuardarCB.Trim(),
                IdFactura = GuardarIdFactura.Value,
                CBPieza = idMoneda == 1 ? cbPieza : cbFactura,
                CNPieza = idMoneda == 1 ? cnPieza : cnFactura,
                IdMoneda = idMoneda,
                TCCosto = tc,
                CBFactura = cbFactura,
                CNFactura = cnFactura,
                DescFactura = descFactura
            };

            await _svc.ActualizarCostosPiezaAsync(dto);
            TempData["Success"] = $"Pieza {GuardarCB} actualizada.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error guardando costos {CB}", GuardarCB);
            TempData["Error"] = $"Error: {ex.Message}";
        }

        return RedirectToPage(new { CodigoBarras = GuardarCB });
    }
}
