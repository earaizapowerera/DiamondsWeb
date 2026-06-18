using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Ventas.CorteDeCaja;

[Authorize]
public class IndexModel : PageModel
{
    private readonly CorteService _corteService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(CorteService corteService, ILogger<IndexModel> logger)
    {
        _corteService = corteService;
        _logger = logger;
    }

    /// <summary>Fecha del último corte registrado (tabla corte).</summary>
    public DateTime? FechaUltimoCorte { get; set; }

    /// <summary>Resumen de ventas desde el último corte hasta hoy.</summary>
    public ResumenVentasPeriodo Resumen { get; set; } = new();

    /// <summary>Desglose de ventas por forma de pago del período actual.</summary>
    public List<VentaPorFormaPago> VentasPorFormaPago { get; set; } = new();

    /// <summary>Historial de cortes realizados.</summary>
    public List<CorteHistorial> Historial { get; set; } = new();

    public async Task OnGetAsync()
    {
        try
        {
            var fechaTask = _corteService.ObtenerFechaUltimoCorteAsync();
            var historialTask = _corteService.ObtenerHistorialAsync();
            await Task.WhenAll(fechaTask, historialTask);

            FechaUltimoCorte = fechaTask.Result;
            Historial = historialTask.Result;

            // Resumen de ventas desde el último corte
            var resumenTask = _corteService.ObtenerResumenVentasAsync(FechaUltimoCorte, null);
            var formaPagoTask = _corteService.ObtenerVentasPorFormaPagoAsync(FechaUltimoCorte, null);
            await Task.WhenAll(resumenTask, formaPagoTask);

            Resumen = resumenTask.Result;
            VentasPorFormaPago = formaPagoTask.Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar pantalla de Corte de Caja");
            TempData["Error"] = $"Error al cargar datos: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostRealizarCorteAsync(string? comentario)
    {
        try
        {
            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid : 1;

            var corte = await _corteService.RealizarCorteAsync(idUsuario, comentario?.Trim());
            TempData["Success"] = $"Corte de caja realizado exitosamente. " +
                $"Período cerrado: {corte.TotalNotas} notas por {corte.TotalVentas:C2}.";

            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al realizar corte de caja");
            TempData["Error"] = $"Error al realizar el corte: {ex.Message}";
            return RedirectToPage();
        }
    }
}
