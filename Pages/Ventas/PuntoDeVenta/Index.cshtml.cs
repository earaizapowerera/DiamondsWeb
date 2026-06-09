using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Ventas.PuntoDeVenta;

[Authorize]
public class IndexModel : PageModel
{
    private readonly SalesService _salesService;
    private readonly InventoryService _inventoryService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(SalesService salesService, InventoryService inventoryService, ILogger<IndexModel> logger)
    {
        _salesService = salesService;
        _inventoryService = inventoryService;
        _logger = logger;
    }

    // Session state
    [BindProperty(SupportsGet = true)]
    public string? IdNota { get; set; }

    public List<PiezaNotaTemporal> PiezasVenta { get; set; } = new();
    public List<PagoNotaTemporal> PagosVenta { get; set; } = new();
    public List<OpcionPago> OpcionesPago { get; set; } = new();

    // Totals
    public decimal SubTotal { get; set; }
    public decimal TotalPagado { get; set; }
    public decimal Cambio { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal DescuentoPorcentaje { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal SobrePrecio { get; set; }

    public decimal Total { get; set; }

    // Input fields
    [BindProperty]
    public string? CodigoBarrasInput { get; set; }

    [BindProperty]
    public int PagoOpcionId { get; set; }

    [BindProperty]
    public decimal PagoImporte { get; set; }

    [BindProperty]
    public decimal? PagoImporteOriginal { get; set; }

    [BindProperty]
    public decimal? PagoTipoCambio { get; set; }

    [BindProperty]
    public string? NombreCliente { get; set; }

    // For removing items
    [BindProperty]
    public string? EliminarCB { get; set; }

    [BindProperty]
    public int EliminarPagoOpcionId { get; set; }

    [BindProperty]
    public decimal EliminarPagoImporte { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            OpcionesPago = await _salesService.ObtenerOpcionesPagoActivasAsync();

            if (!string.IsNullOrWhiteSpace(IdNota))
            {
                await CargarDatosVentaAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar punto de venta");
            TempData["Error"] = $"Error al cargar: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostCrearSesionAsync()
    {
        try
        {
            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid : 1;
            var idNota = await _salesService.CrearSesionVentaAsync(idUsuario);
            TempData["Success"] = $"Sesion de venta {idNota} iniciada.";
            return RedirectToPage(new { IdNota = idNota });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear sesion de venta");
            TempData["Error"] = $"Error al crear sesion: {ex.Message}";
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostAgregarPiezaAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(IdNota))
            {
                TempData["Error"] = "Debe iniciar una sesion de venta primero.";
                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(CodigoBarrasInput))
            {
                TempData["Error"] = "Ingrese un codigo de barras.";
                return RedirectToPage(new { IdNota, DescuentoPorcentaje, SobrePrecio });
            }

            var cb = CodigoBarrasInput.Trim();

            // Verify piece exists
            var cbEncontrado = await _salesService.BuscarPiezaParaVentaAsync(cb);
            if (cbEncontrado == null)
            {
                TempData["Error"] = $"Pieza {cb} no encontrada en el sistema.";
                return RedirectToPage(new { IdNota, DescuentoPorcentaje, SobrePrecio });
            }

            // Get piece details for price
            var pieza = await _inventoryService.ObtenerPiezaAsync(cbEncontrado);
            var descripcion = pieza?.Descripcion ?? "Sin descripcion";
            var precio = pieza?.Precio ?? 0;

            await _salesService.AgregarPiezaVentaAsync(IdNota, cbEncontrado, descripcion, precio, precio);
            TempData["Success"] = $"Pieza {cbEncontrado} agregada.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al agregar pieza a venta");
            TempData["Error"] = $"Error al agregar pieza: {ex.Message}";
        }

        return RedirectToPage(new { IdNota, DescuentoPorcentaje, SobrePrecio });
    }

    public async Task<IActionResult> OnPostEliminarPiezaAsync()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(IdNota) && !string.IsNullOrWhiteSpace(EliminarCB))
            {
                await _salesService.EliminarPiezaVentaAsync(IdNota, EliminarCB);
                TempData["Success"] = $"Pieza {EliminarCB} eliminada de la venta.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar pieza de venta");
            TempData["Error"] = $"Error al eliminar pieza: {ex.Message}";
        }

        return RedirectToPage(new { IdNota, DescuentoPorcentaje, SobrePrecio });
    }

    public async Task<IActionResult> OnPostAgregarPagoAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(IdNota))
            {
                TempData["Error"] = "No hay sesion de venta activa.";
                return RedirectToPage();
            }

            if (PagoImporte <= 0)
            {
                TempData["Error"] = "El importe del pago debe ser mayor a cero.";
                return RedirectToPage(new { IdNota, DescuentoPorcentaje, SobrePrecio });
            }

            await _salesService.AgregarPagoAsync(IdNota, PagoOpcionId, PagoImporte, PagoImporteOriginal, PagoTipoCambio);
            TempData["Success"] = "Pago agregado.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al agregar pago");
            TempData["Error"] = $"Error al agregar pago: {ex.Message}";
        }

        return RedirectToPage(new { IdNota, DescuentoPorcentaje, SobrePrecio });
    }

    public async Task<IActionResult> OnPostEliminarPagoAsync()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(IdNota))
            {
                await _salesService.EliminarPagoAsync(IdNota, EliminarPagoOpcionId, EliminarPagoImporte);
                TempData["Success"] = "Pago eliminado.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar pago");
            TempData["Error"] = $"Error al eliminar pago: {ex.Message}";
        }

        return RedirectToPage(new { IdNota, DescuentoPorcentaje, SobrePrecio });
    }

    public async Task<IActionResult> OnPostCerrarNotaAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(IdNota))
            {
                TempData["Error"] = "No hay sesion de venta activa.";
                return RedirectToPage();
            }

            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid : 1;
            var nombre = string.IsNullOrWhiteSpace(NombreCliente) ? "Publico en General" : NombreCliente.Trim();

            var resultado = await _salesService.CerrarNotaAsync(IdNota, nombre, DateTime.Now, idUsuario);
            TempData["Success"] = resultado;
            return RedirectToPage(new { IdNota = (string?)null });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cerrar nota");
            TempData["Error"] = $"Error al cerrar nota: {ex.Message}";
            return RedirectToPage(new { IdNota, DescuentoPorcentaje, SobrePrecio });
        }
    }

    private async Task CargarDatosVentaAsync()
    {
        PiezasVenta = await _salesService.ObtenerPiezasVentaAsync(IdNota!);
        PagosVenta = await _salesService.ObtenerPagosVentaAsync(IdNota!);
        OpcionesPago = await _salesService.ObtenerOpcionesPagoActivasAsync();

        SubTotal = PiezasVenta.Sum(p => p.Total);
        var descuento = SubTotal * (DescuentoPorcentaje / 100m);
        Total = SubTotal - descuento + SobrePrecio;
        TotalPagado = PagosVenta.Sum(p => p.Importe);
        Cambio = TotalPagado - Total;
    }
}
