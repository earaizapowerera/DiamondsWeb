using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace DiamondsWeb.Pages.Actualizaciones;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ActualizacionesService _svc;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ActualizacionesService svc, ILogger<IndexModel> logger)
    {
        _svc = svc;
        _logger = logger;
    }

    // ── Datos para la vista ──
    public FacturaDto? FacturaActual { get; set; }
    public List<PiezaDisponibleDto> PiezasDisponibles { get; set; } = new();
    public List<PiezaVinculadaDto> PiezasVinculadas { get; set; } = new();
    public FacturaTotalesDto Totales { get; set; } = new();
    public List<ProveedorComboDto> Proveedores { get; set; } = new();
    public List<RazonSocialComboDto> RazonesSociales { get; set; } = new();
    public List<FacturaDto> FacturasRecientes { get; set; } = new();

    public string? Mensaje { get; set; }
    public string? MensajeTipo { get; set; }

    // ── Parámetros de navegación ──
    [BindProperty(SupportsGet = true)]
    public int? IdFactura { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FiltroPiezas { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FiltroFacturas { get; set; }

    // ── Helpers de autenticación ──
    private int ObtenerIdUsuario()
    {
        var claim = User.FindFirst("IdUsuario")?.Value
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 1;
    }

    /// <summary>
    /// GET: Carga la pantalla con la factura seleccionada (si hay).
    /// </summary>
    public async Task OnGetAsync()
    {
        await CargarProveedoresAsync();

        if (IdFactura.HasValue && IdFactura.Value > 0)
        {
            FacturaActual = await _svc.ObtenerFacturaAsync(IdFactura.Value);
            if (FacturaActual != null)
            {
                PiezasVinculadas = await _svc.ObtenerPiezasVinculadasAsync(IdFactura.Value);
                Totales = await _svc.ObtenerTotalesFacturaAsync(IdFactura.Value);

                if (FacturaActual.Proveedor.HasValue)
                    RazonesSociales = await _svc.ObtenerRazonesSocialesAsync(
                        FacturaActual.Proveedor.Value);

                if (!string.IsNullOrWhiteSpace(FiltroPiezas))
                    PiezasDisponibles = await _svc.BuscarPiezasDisponiblesAsync(
                        IdFactura.Value, FiltroPiezas);
            }
            else
            {
                Mensaje = $"No se encontró la factura {IdFactura.Value}.";
                MensajeTipo = "warning";
            }
        }

        FacturasRecientes = await _svc.BuscarFacturasAsync(FiltroFacturas);
    }

    // ── POST handlers ──

    public IActionResult OnPostBuscarPiezas(int idFactura, string? filtroPiezas)
    {
        return RedirectToPage(new { IdFactura = idFactura, FiltroPiezas = filtroPiezas });
    }

    public IActionResult OnPostBuscarFactura(string? filtroFacturas)
    {
        return RedirectToPage(new { FiltroFacturas = filtroFacturas });
    }

    public IActionResult OnPostSeleccionarFactura(int idFactura)
    {
        return RedirectToPage(new { IdFactura = idFactura });
    }

    public async Task<IActionResult> OnPostCrearFacturaAsync(
        string folioFactura, DateTime fechaFactura,
        int proveedor, int idRazonSocialProveedor, string? pedimento)
    {
        try
        {
            var req = new FacturaFormRequest
            {
                FolioFactura = folioFactura,
                FechaFactura = fechaFactura,
                Proveedor = proveedor,
                IdRazonSocialProveedor = idRazonSocialProveedor,
                Pedimento = pedimento
            };

            var idUsuario = ObtenerIdUsuario();
            var idTienda = await _svc.ObtenerIdTiendaAsync();
            var nuevoId = await _svc.CrearFacturaAsync(req, idUsuario, idTienda);
            TempData["Mensaje"] = $"Factura #{nuevoId} creada (folio {folioFactura}).";
            TempData["MensajeTipo"] = "success";
            return RedirectToPage(new { IdFactura = nuevoId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear factura");
            TempData["Mensaje"] = $"Error al crear factura: {ex.Message}";
            TempData["MensajeTipo"] = "danger";
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostEditarFacturaAsync(
        int idFactura, string folioFactura, DateTime fechaFactura,
        int proveedor, int idRazonSocialProveedor, string? pedimento)
    {
        try
        {
            var req = new FacturaFormRequest
            {
                FolioFactura = folioFactura,
                FechaFactura = fechaFactura,
                Proveedor = proveedor,
                IdRazonSocialProveedor = idRazonSocialProveedor,
                Pedimento = pedimento
            };

            await _svc.ActualizarFacturaAsync(idFactura, req);
            TempData["Mensaje"] = "Factura actualizada correctamente.";
            TempData["MensajeTipo"] = "success";
            return RedirectToPage(new { IdFactura = idFactura });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al editar factura {Id}", idFactura);
            TempData["Mensaje"] = $"Error al editar factura: {ex.Message}";
            TempData["MensajeTipo"] = "danger";
            return RedirectToPage(new { IdFactura = idFactura });
        }
    }

    public async Task<IActionResult> OnPostEliminarFacturaAsync(int idFactura)
    {
        var (ok, mensaje) = await _svc.EliminarFacturaAsync(idFactura);
        TempData["Mensaje"] = mensaje;
        TempData["MensajeTipo"] = ok ? "success" : "danger";
        return ok ? RedirectToPage() : RedirectToPage(new { IdFactura = idFactura });
    }

    public async Task<IActionResult> OnPostAsignarPiezaAsync(
        int idFactura, string codigoBarras,
        decimal cbTotal, decimal cnTotal, decimal tcCosto,
        string? filtroPiezas)
    {
        var req = new AsignarPiezaRequest
        {
            CodigoBarras = codigoBarras,
            IdFactura = idFactura,
            CBTotal = cbTotal,
            CNTotal = cnTotal,
            TCCosto = tcCosto
        };

        var (ok, mensaje) = await _svc.AsignarPiezaAsync(req);
        TempData["Mensaje"] = mensaje;
        TempData["MensajeTipo"] = ok ? "success" : "danger";
        return RedirectToPage(new { IdFactura = idFactura, FiltroPiezas = filtroPiezas });
    }

    public async Task<IActionResult> OnPostAsignarRemisionAsync(
        int idFactura, int idRemision, decimal tipoCambio,
        string? filtroPiezas)
    {
        var (ok, mensaje, _) = await _svc.AsignarRemisionCompletaAsync(
            idFactura, idRemision, tipoCambio);

        TempData["Mensaje"] = mensaje;
        TempData["MensajeTipo"] = ok ? "success" : "danger";
        return RedirectToPage(new { IdFactura = idFactura, FiltroPiezas = filtroPiezas });
    }

    public async Task<IActionResult> OnPostQuitarPiezaAsync(
        int idFactura, string codigoBarras)
    {
        var (ok, mensaje) = await _svc.QuitarPiezaAsync(idFactura, codigoBarras);
        TempData["Mensaje"] = mensaje;
        TempData["MensajeTipo"] = ok ? "success" : "danger";
        return RedirectToPage(new { IdFactura = idFactura });
    }

    // ── API endpoints para AJAX ──

    public async Task<IActionResult> OnGetRazonesSocialesAsync(int proveedor)
    {
        var lista = await _svc.ObtenerRazonesSocialesAsync(proveedor);
        return new JsonResult(lista);
    }

    private async Task CargarProveedoresAsync()
    {
        Proveedores = await _svc.ObtenerProveedoresAsync();
    }
}
