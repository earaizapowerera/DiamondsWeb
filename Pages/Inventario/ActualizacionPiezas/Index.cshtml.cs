using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DiamondsWeb.Models;
using DiamondsWeb.Services;

namespace DiamondsWeb.Pages.Inventario.ActualizacionPiezas;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ActualizacionService _service;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ActualizacionService service, ILogger<IndexModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    // ── Datos para la vista ──
    public List<PiezaActualizacion> Piezas { get; set; } = new();
    public List<MonedaCatalogo> Monedas { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    // ── Filtro de búsqueda ──
    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    // ── Pieza seleccionada (para el formulario de costos) ──
    [BindProperty(SupportsGet = true)]
    public string? PiezaSeleccionada { get; set; }

    public PiezaActualizacion? PiezaActual { get; set; }
    public FacturaBusqueda? FacturaActual { get; set; }

    // ── Bind properties para guardar costos ──
    [BindProperty] public string GuardarCB { get; set; } = "";
    [BindProperty] public int GuardarIdFactura { get; set; }
    [BindProperty] public decimal GuardarCBPieza { get; set; }
    [BindProperty] public decimal GuardarCNPieza { get; set; }
    [BindProperty] public int GuardarIdMoneda { get; set; }
    [BindProperty] public decimal GuardarTCCosto { get; set; }
    [BindProperty] public decimal GuardarCBFactura { get; set; }
    [BindProperty] public decimal GuardarCNFactura { get; set; }
    [BindProperty] public decimal GuardarDescFactura { get; set; }

    // ── Bind properties para crear factura ──
    [BindProperty] public string NuevaFolioFactura { get; set; } = "";
    [BindProperty] public int NuevaProveedor { get; set; }
    [BindProperty] public int NuevaIdRazonSocial { get; set; }
    [BindProperty] public DateTime NuevaFechaFactura { get; set; } = DateTime.UtcNow;
    [BindProperty] public string NuevaPiezaCB { get; set; } = "";

    public async Task OnGetAsync()
    {
        Monedas = await _service.ObtenerMonedasAsync();

        if (!string.IsNullOrWhiteSpace(Buscar))
        {
            try
            {
                Piezas = await _service.BuscarPiezasAsync(Buscar);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error buscando piezas con '{Buscar}'", Buscar);
                ErrorMessage = $"Error al buscar: {ex.Message}";
            }
        }

        // Si hay pieza seleccionada, cargar sus datos
        if (!string.IsNullOrWhiteSpace(PiezaSeleccionada) && Piezas.Any())
        {
            PiezaActual = Piezas.FirstOrDefault(
                p => p.CodigoBarras == PiezaSeleccionada);

            if (PiezaActual?.IdFactura.HasValue == true && PiezaActual.IdFactura > 0)
            {
                FacturaActual = await _service.ObtenerFacturaPorIdAsync(
                    PiezaActual.IdFactura.Value);
            }
        }
    }

    /// <summary>
    /// Handler: Buscar factura por folio para la pieza seleccionada
    /// </summary>
    public async Task<IActionResult> OnPostBuscarFacturaAsync(
        string codigoBarras, string folioFactura, int proveedor, string? buscar)
    {
        Monedas = await _service.ObtenerMonedasAsync();

        if (!string.IsNullOrWhiteSpace(buscar))
            Piezas = await _service.BuscarPiezasAsync(buscar);

        Buscar = buscar;
        PiezaSeleccionada = codigoBarras;
        PiezaActual = Piezas.FirstOrDefault(p => p.CodigoBarras == codigoBarras);

        if (string.IsNullOrWhiteSpace(folioFactura))
        {
            ErrorMessage = "Ingrese un folio de factura.";
            return Page();
        }

        var factura = await _service.BuscarFacturaPorFolioYProveedorAsync(
            folioFactura, proveedor);

        if (factura != null)
        {
            FacturaActual = factura;
            SuccessMessage = $"Factura encontrada: {factura.FolioFactura} — {factura.RazonSocialProveedor}";
        }
        else
        {
            // No existe, mostrar formulario de alta
            ErrorMessage = $"No existe factura '{folioFactura}' para proveedor {proveedor}. Puede crear una nueva abajo.";
        }

        return Page();
    }

    /// <summary>
    /// Handler: Crear nueva factura
    /// </summary>
    public async Task<IActionResult> OnPostCrearFacturaAsync()
    {
        try
        {
            var idUsuario = int.TryParse(
                User.FindFirst("IdUsuario")?.Value, out var uid) ? uid
                : throw new UnauthorizedAccessException("IdUsuario claim not found");
            var idTienda = int.TryParse(
                User.FindFirst("IdTienda")?.Value, out var tid) ? tid : 1;

            var idFactura = await _service.CrearFacturaAsync(
                NuevaFolioFactura, NuevaProveedor, NuevaIdRazonSocial,
                NuevaFechaFactura, idUsuario, idTienda);

            TempData["Success"] = $"Factura creada con ID {idFactura}.";

            return RedirectToPage(new
            {
                Buscar,
                PiezaSeleccionada = NuevaPiezaCB
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creando factura");
            ErrorMessage = $"Error al crear factura: {ex.Message}";
            Monedas = await _service.ObtenerMonedasAsync();
            if (!string.IsNullOrWhiteSpace(Buscar))
                Piezas = await _service.BuscarPiezasAsync(Buscar);
            return Page();
        }
    }

    /// <summary>
    /// Handler: Guardar actualización de costos
    /// </summary>
    public async Task<IActionResult> OnPostGuardarCostosAsync()
    {
        try
        {
            var dto = new ActualizarCostoPiezaDto
            {
                CodigoBarras = GuardarCB,
                IdFactura = GuardarIdFactura,
                CBPieza = GuardarCBPieza,
                CNPieza = GuardarCNPieza,
                IdMoneda = GuardarIdMoneda,
                TCCosto = GuardarTCCosto,
                CBFactura = GuardarCBFactura,
                CNFactura = GuardarCNFactura,
                DescFactura = GuardarDescFactura
            };

            await _service.ActualizarCostosPiezaAsync(dto);
            TempData["Success"] = $"Costos actualizados para pieza {GuardarCB}.";

            return RedirectToPage(new { Buscar });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando costos de pieza {CB}", GuardarCB);
            ErrorMessage = $"Error al guardar: {ex.Message}";
            Monedas = await _service.ObtenerMonedasAsync();
            if (!string.IsNullOrWhiteSpace(Buscar))
                Piezas = await _service.BuscarPiezasAsync(Buscar);
            return Page();
        }
    }

    /// <summary>
    /// API: Obtener razones sociales de proveedor (para dropdown dinámico)
    /// </summary>
    public async Task<IActionResult> OnGetRazonesSocialesAsync(int proveedor)
    {
        var razones = await _service.ObtenerRazonesSocialesPorProveedorAsync(proveedor);
        return new JsonResult(razones);
    }
}
