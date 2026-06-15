using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Catalogos;

[Authorize]
public class MonedasModel : PageModel
{
    private readonly MonedaService _monedaService;
    private readonly ILogger<MonedasModel> _logger;

    public MonedasModel(MonedaService monedaService, ILogger<MonedasModel> logger)
    {
        _monedaService = monedaService;
        _logger = logger;
    }

    public List<MonedaDetalle> Monedas { get; set; } = new();

    [TempData]
    public string? MensajeExito { get; set; }

    [TempData]
    public string? MensajeError { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Monedas = await _monedaService.ObtenerTodasAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar monedas");
            MensajeError = $"Error al cargar monedas: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostGuardarAsync(int idMoneda, string nombre, bool extranjera)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            MensajeError = "El nombre de la moneda es obligatorio.";
            return RedirectToPage();
        }

        try
        {
            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid
                : throw new UnauthorizedAccessException("IdUsuario claim not found");

            if (idMoneda > 0)
            {
                await _monedaService.ActualizarAsync(idMoneda, nombre, extranjera, idUsuario);
                MensajeExito = $"Moneda '{nombre}' actualizada correctamente.";
            }
            else
            {
                await _monedaService.CrearAsync(nombre, extranjera, idUsuario);
                MensajeExito = $"Moneda '{nombre}' registrada correctamente.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar moneda");
            MensajeError = $"Error al guardar: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEliminarAsync(int idMoneda)
    {
        try
        {
            var moneda = await _monedaService.ObtenerPorIdAsync(idMoneda);
            await _monedaService.EliminarAsync(idMoneda);
            MensajeExito = $"Moneda '{moneda?.Nombre ?? idMoneda.ToString()}' eliminada correctamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar moneda {Id}", idMoneda);
            MensajeError = $"Error al eliminar: {ex.Message}";
        }

        return RedirectToPage();
    }
}
