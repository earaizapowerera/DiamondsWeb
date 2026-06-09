using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Catalogos;

[Authorize]
public class OpcionesPagoModel : PageModel
{
    private readonly OpcionPagoService _service;
    private readonly ILogger<OpcionesPagoModel> _logger;

    public OpcionesPagoModel(OpcionPagoService service, ILogger<OpcionesPagoModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    public List<OpcionPago> Opciones { get; set; } = new();
    public List<MonedaItem> Monedas { get; set; } = new();

    [TempData]
    public string? MensajeExito { get; set; }

    [TempData]
    public string? MensajeError { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Opciones = await _service.ObtenerTodasAsync();
            Monedas = await _service.ObtenerMonedasAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar opciones de pago");
            MensajeError = $"Error al cargar opciones de pago: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostGuardarAsync(
        int idOpcionPago, string nombre, int idMoneda, string? logo, bool activa)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            MensajeError = "El nombre de la opcion de pago es obligatorio.";
            return RedirectToPage();
        }

        try
        {
            if (idOpcionPago > 0)
            {
                await _service.ActualizarAsync(idOpcionPago, nombre, idMoneda, logo, activa);
                MensajeExito = $"Opcion de pago '{nombre}' actualizada correctamente.";
            }
            else
            {
                await _service.CrearAsync(nombre, idMoneda, logo, activa);
                MensajeExito = $"Opcion de pago '{nombre}' registrada correctamente.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar opcion de pago");
            MensajeError = $"Error al guardar: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleActivaAsync(int idOpcionPago, bool activaActual)
    {
        try
        {
            var nuevaActiva = !activaActual;
            await _service.CambiarActivaAsync(idOpcionPago, nuevaActiva);
            var estado = nuevaActiva ? "activada" : "desactivada";
            MensajeExito = $"Opcion de pago {estado} correctamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar estado de opcion de pago {Id}", idOpcionPago);
            MensajeError = $"Error al cambiar estado: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEliminarAsync(int idOpcionPago)
    {
        try
        {
            var opcion = await _service.ObtenerPorIdAsync(idOpcionPago);
            await _service.EliminarAsync(idOpcionPago);
            MensajeExito = $"Opcion de pago '{opcion?.Nombre ?? idOpcionPago.ToString()}' eliminada correctamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar opcion de pago {Id}", idOpcionPago);
            MensajeError = $"Error al eliminar: {ex.Message}";
        }

        return RedirectToPage();
    }
}
