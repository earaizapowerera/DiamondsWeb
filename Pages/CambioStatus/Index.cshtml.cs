using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.CambioStatus;

[Authorize]
public class IndexModel : PageModel
{
    private readonly CambioStatusService _service;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(CambioStatusService service, ILogger<IndexModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    // --- Catálogo de status ---
    public List<StatusPieza> StatusList { get; set; } = new();

    // --- Pieza escaneada ---
    public PiezaStatus? PiezaActual { get; set; }

    // --- Grid de piezas fuera de Exhibición ---
    public List<PiezaEnStatus> PiezasEnStatus { get; set; } = new();

    // --- Bitácora reciente ---
    public List<BitacoraStatus> Bitacora { get; set; } = new();

    // --- Mensajes UI ---
    public string? MensajeExito { get; set; }
    public string? MensajeError { get; set; }

    // --- Filtros GET ---
    [BindProperty(SupportsGet = true)]
    public string? CodigoBarras { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? FiltroStatus { get; set; }

    public async Task OnGetAsync()
    {
        StatusList = await _service.ObtenerStatusAsync();
        PiezasEnStatus = await _service.ObtenerPiezasEnStatusAsync(FiltroStatus);
        Bitacora = await _service.ObtenerBitacoraAsync();

        if (!string.IsNullOrWhiteSpace(CodigoBarras))
        {
            PiezaActual = await _service.BuscarPiezaAsync(CodigoBarras.Trim());
            if (PiezaActual is null)
                MensajeError = $"La pieza '{CodigoBarras}' no existe o no tiene status asignado.";
        }
    }

    public async Task<IActionResult> OnPostCambiarStatusAsync(
        string codigoBarras, int nuevoStatusId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(codigoBarras))
            {
                MensajeError = "Ingrese un código de barras.";
                await CargarDatos();
                return Page();
            }

            var idCambio = await _service.CambiarStatusAsync(
                codigoBarras.Trim(), nuevoStatusId, userId: 1);

            MensajeExito = $"Status cambiado exitosamente. Id de Cambio: {idCambio}";

            // Recargar con la pieza actualizada
            CodigoBarras = codigoBarras.Trim();
            await CargarDatos();
            PiezaActual = await _service.BuscarPiezaAsync(CodigoBarras);
            return Page();
        }
        catch (InvalidOperationException ex)
        {
            MensajeError = ex.Message;
            CodigoBarras = codigoBarras?.Trim();
            await CargarDatos();
            PiezaActual = await _service.BuscarPiezaAsync(CodigoBarras ?? "");
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar status de {CB}", codigoBarras);
            MensajeError = "Error inesperado al cambiar el status.";
            CodigoBarras = codigoBarras?.Trim();
            await CargarDatos();
            return Page();
        }
    }

    /// <summary>AJAX: buscar pieza por código de barras</summary>
    public async Task<IActionResult> OnGetBuscarPiezaAsync(string cb)
    {
        if (string.IsNullOrWhiteSpace(cb))
            return new JsonResult(new { found = false });

        var pieza = await _service.BuscarPiezaAsync(cb.Trim());
        if (pieza is null)
            return new JsonResult(new { found = false });

        return new JsonResult(new
        {
            found = true,
            pieza.CodigoBarras,
            pieza.Descripcion,
            pieza.IdStatus,
            pieza.NombreStatus,
            fechaUltimoCambio = pieza.FechaUltimoCambio?.ToString("dd/MM/yyyy HH:mm")
        });
    }

    private async Task CargarDatos()
    {
        StatusList = await _service.ObtenerStatusAsync();
        PiezasEnStatus = await _service.ObtenerPiezasEnStatusAsync(FiltroStatus);
        Bitacora = await _service.ObtenerBitacoraAsync();
    }
}
