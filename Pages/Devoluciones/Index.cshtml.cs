using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Devoluciones;

[Authorize]
public class IndexModel : PageModel
{
    private readonly DevolucionService _devolucionService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(DevolucionService devolucionService, ILogger<IndexModel> logger)
    {
        _devolucionService = devolucionService;
        _logger = logger;
    }

    public List<DevolucionItem> Devoluciones { get; set; } = new();
    public DevolucionStats Stats { get; set; } = new();
    public string? MensajeExito { get; set; }
    public string? MensajeError { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? BuscarTexto { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FiltroRemision { get; set; }

    public async Task OnGetAsync()
    {
        await CargarDatosAsync();
    }

    /// <summary>
    /// Registrar devolucion: valida pieza, backup etiquetas, inserta, ejecuta sp_devolucion
    /// </summary>
    public async Task<IActionResult> OnPostRegistrarAsync(
        string codigoBarras, string motivo, string? remision)
    {
        if (string.IsNullOrWhiteSpace(codigoBarras))
        {
            MensajeError = "Debe ingresar un codigo de barras.";
            await CargarDatosAsync();
            return Page();
        }

        if (string.IsNullOrWhiteSpace(motivo))
        {
            MensajeError = "Debe ingresar un motivo de devolucion.";
            await CargarDatosAsync();
            return Page();
        }

        // userId del usuario logueado (claim de UserPortal)
        var userIdClaim = User.FindFirst("UserId")?.Value
                          ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var userId = int.TryParse(userIdClaim, out var uid) ? uid : 1;

        if (string.IsNullOrWhiteSpace(remision))
        {
            // Advertencia silenciosa: queda pendiente de remision (igual que VB6)
            remision = null;
        }

        var (exito, mensaje) = await _devolucionService.RegistrarDevolucionAsync(
            codigoBarras.Trim(), motivo.Trim(), remision?.Trim(), userId);

        if (exito)
            MensajeExito = mensaje;
        else
            MensajeError = mensaje;

        await CargarDatosAsync();
        return Page();
    }

    /// <summary>
    /// Aplicar remision/nota de credito a devoluciones seleccionadas
    /// </summary>
    public async Task<IActionResult> OnPostAplicarRemisionAsync(
        string remision, List<string> seleccionados)
    {
        var (exito, mensaje) = await _devolucionService.AplicarRemisionAsync(
            remision?.Trim() ?? "", seleccionados);

        if (exito)
            MensajeExito = mensaje;
        else
            MensajeError = mensaje;

        await CargarDatosAsync();
        return Page();
    }

    /// <summary>
    /// Eliminar devolucion y restaurar pieza al inventario
    /// </summary>
    public async Task<IActionResult> OnPostEliminarAsync(string codigoBarras)
    {
        var (exito, mensaje) = await _devolucionService.EliminarDevolucionAsync(codigoBarras);

        if (exito)
            MensajeExito = mensaje;
        else
            MensajeError = mensaje;

        await CargarDatosAsync();
        return Page();
    }

    /// <summary>
    /// AJAX: Valida si una pieza existe en inventario
    /// </summary>
    public async Task<IActionResult> OnGetValidarPiezaAsync(string cb)
    {
        if (string.IsNullOrWhiteSpace(cb))
            return new JsonResult(new { existe = false, mensaje = "Codigo vacio" });

        var pieza = await _devolucionService.ValidarPiezaAsync(cb.Trim());

        if (pieza == null)
            return new JsonResult(new { existe = false, mensaje = "Pieza no encontrada en inventario" });

        return new JsonResult(new
        {
            existe = true,
            descripcion = pieza.Descripcion,
            precio = pieza.Precio,
            fechaCaptura = pieza.FechaCaptura.ToString("dd/MM/yyyy")
        });
    }

    private async Task CargarDatosAsync()
    {
        try
        {
            Devoluciones = await _devolucionService.ObtenerDevolucionesAsync(BuscarTexto, FiltroRemision);
            Stats = await _devolucionService.ObtenerEstadisticasAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cargando datos de devoluciones");
            MensajeError = $"Error al consultar datos: {ex.Message}";
        }
    }
}
