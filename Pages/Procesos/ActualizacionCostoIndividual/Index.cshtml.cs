using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Procesos.ActualizacionCostoIndividual;

/// <summary>
/// Actualización de Costos Individual — migración de frmActualizacionesII.frm (VB6).
/// Flujo: Buscar piezas → seleccionar pieza → ingresar folio factura →
/// si no existe la factura la crea → muestra costos → guardar actualización.
/// </summary>
[Authorize]
public class IndexModel : PageModel
{
    private readonly ActualizacionService _actualizacionService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ActualizacionService actualizacionService, ILogger<IndexModel> logger)
    {
        _actualizacionService = actualizacionService;
        _logger = logger;
    }

    // ── Búsqueda de piezas ──
    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    public List<PiezaActualizacion> Piezas { get; set; } = new();

    // ── Pieza seleccionada ──
    [BindProperty(SupportsGet = true)]
    public string? CodigoBarrasSeleccionado { get; set; }

    public PiezaActualizacion? PiezaSeleccionada { get; set; }

    // ── Factura ──
    [BindProperty(SupportsGet = true)]
    public string? FolioFactura { get; set; }

    public FacturaBusqueda? FacturaEncontrada { get; set; }
    public bool MostrarFormFactura { get; set; }

    // ── Costos ──
    public List<MonedaCatalogo> Monedas { get; set; } = new();
    public List<RazonSocialCatalogo> RazonesSociales { get; set; } = new();

    // ── GET: Carga de página ──
    public async Task OnGetAsync()
    {
        try
        {
            Monedas = await _actualizacionService.ObtenerMonedasAsync();

            // 1. Buscar piezas si hay término de búsqueda
            if (!string.IsNullOrWhiteSpace(Buscar))
            {
                Piezas = await _actualizacionService.BuscarPiezasAsync(Buscar.Trim());
                if (Piezas.Count == 0)
                    TempData["Warning"] = "No se encontraron piezas con ese criterio.";
            }

            // 2. Si hay pieza seleccionada, cargar sus datos
            if (!string.IsNullOrWhiteSpace(CodigoBarrasSeleccionado))
            {
                PiezaSeleccionada = Piezas.FirstOrDefault(
                    p => p.CodigoBarras == CodigoBarrasSeleccionado);

                if (PiezaSeleccionada == null && !string.IsNullOrWhiteSpace(Buscar))
                {
                    // Buscar directamente si no está en la lista
                    var resultado = await _actualizacionService.BuscarPiezasAsync(
                        CodigoBarrasSeleccionado);
                    PiezaSeleccionada = resultado.FirstOrDefault();
                }

                // Cargar razones sociales del proveedor de la pieza
                if (PiezaSeleccionada?.Proveedor > 0)
                {
                    RazonesSociales = await _actualizacionService
                        .ObtenerRazonesSocialesPorProveedorAsync(PiezaSeleccionada.Proveedor.Value);
                }

                // 3. Si hay folio de factura, buscarla
                if (!string.IsNullOrWhiteSpace(FolioFactura) && PiezaSeleccionada?.Proveedor > 0)
                {
                    FacturaEncontrada = await _actualizacionService
                        .BuscarFacturaPorFolioYProveedorAsync(
                            FolioFactura.Trim(), PiezaSeleccionada.Proveedor.Value);

                    if (FacturaEncontrada == null)
                    {
                        // Factura no existe — mostrar formulario de alta
                        MostrarFormFactura = true;
                    }
                }
                else if (PiezaSeleccionada?.IdFactura > 0 && string.IsNullOrWhiteSpace(FolioFactura))
                {
                    // Pieza ya tiene factura asignada, cargarla
                    FacturaEncontrada = await _actualizacionService
                        .ObtenerFacturaPorIdAsync(PiezaSeleccionada.IdFactura.Value);
                    if (FacturaEncontrada != null)
                        FolioFactura = FacturaEncontrada.FolioFactura;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en carga de Actualización de Costos Individual");
            TempData["Error"] = $"Error al cargar datos: {ex.Message}";
        }
    }

    // ── POST: Crear factura nueva ──
    public async Task<IActionResult> OnPostCrearFacturaAsync(
        string codigoBarras, string buscar, string folioFactura,
        int proveedor, int idRazonSocial, DateTime fechaFactura)
    {
        try
        {
            var idUsuario = ObtenerIdUsuario();
            var idTienda = ObtenerIdTienda();

            var idFactura = await _actualizacionService.CrearFacturaAsync(
                folioFactura, proveedor, idRazonSocial, fechaFactura, idUsuario, idTienda);

            TempData["Success"] = $"Factura creada exitosamente (ID: {idFactura}).";

            return RedirectToPage(new
            {
                Buscar = buscar,
                CodigoBarrasSeleccionado = codigoBarras,
                FolioFactura = folioFactura
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear factura");
            TempData["Error"] = $"Error al crear factura: {ex.Message}";
            return RedirectToPage(new
            {
                Buscar = buscar,
                CodigoBarrasSeleccionado = codigoBarras
            });
        }
    }

    // ── POST: Guardar actualización de costos ──
    public async Task<IActionResult> OnPostGuardarAsync(
        string codigoBarras, string buscar, int idFactura,
        decimal cbPieza, decimal cnPieza, int idMoneda, decimal tcCosto,
        decimal cbFactura, decimal cnFactura, decimal descFactura)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(codigoBarras))
            {
                TempData["Error"] = "Código de barras requerido.";
                return RedirectToPage();
            }

            var dto = new ActualizarCostoPiezaDto
            {
                CodigoBarras = codigoBarras,
                IdFactura = idFactura,
                CBPieza = cbPieza,
                CNPieza = cnPieza,
                IdMoneda = idMoneda,
                TCCosto = tcCosto,
                CBFactura = cbFactura,
                CNFactura = cnFactura,
                DescFactura = descFactura
            };

            await _actualizacionService.ActualizarCostosPiezaAsync(dto);

            _logger.LogInformation(
                "Costos actualizados para pieza {CB} por usuario {User}",
                codigoBarras, ObtenerIdUsuario());

            TempData["Success"] = $"Costos de pieza {codigoBarras} actualizados exitosamente.";

            return RedirectToPage(new { Buscar = buscar, CodigoBarrasSeleccionado = codigoBarras });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar costos de pieza {CB}", codigoBarras);
            TempData["Error"] = $"Error al actualizar costos: {ex.Message}";
            return RedirectToPage(new { Buscar = buscar, CodigoBarrasSeleccionado = codigoBarras });
        }
    }

    private int ObtenerIdUsuario()
    {
        return int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid)
            ? uid
            : throw new UnauthorizedAccessException("IdUsuario claim not found");
    }

    private int ObtenerIdTienda()
    {
        return int.TryParse(User.FindFirst("IdTienda")?.Value, out var tid) ? tid : 1;
    }
}
