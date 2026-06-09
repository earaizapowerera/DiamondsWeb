using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Ventas.Devoluciones;

[Authorize]
public class IndexModel : PageModel
{
    private readonly SalesService _salesService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(SalesService salesService, ILogger<IndexModel> logger)
    {
        _salesService = salesService;
        _logger = logger;
    }

    public List<Devolucion> Devoluciones { get; set; } = new();

    [BindProperty]
    public string NuevoCodigoBarras { get; set; } = "";

    [BindProperty]
    public string NuevoMotivo { get; set; } = "";

    [BindProperty]
    public int? RemisionDevolucionId { get; set; }

    [BindProperty]
    public string? RemisionValor { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Devoluciones = await _salesService.ObtenerDevolucionesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar devoluciones");
            TempData["Error"] = $"Error al cargar devoluciones: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostCrearAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NuevoCodigoBarras) || string.IsNullOrWhiteSpace(NuevoMotivo))
            {
                TempData["Error"] = "El codigo de barras y el motivo son requeridos.";
                return RedirectToPage();
            }

            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid : 1;
            var resultado = await _salesService.CrearDevolucionAsync(NuevoCodigoBarras.Trim(), NuevoMotivo.Trim(), idUsuario);

            if (resultado == "Pieza no encontrada")
                TempData["Error"] = resultado;
            else
                TempData["Success"] = resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear devolucion");
            TempData["Error"] = $"Error al crear devolucion: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemisionAsync()
    {
        try
        {
            if (RemisionDevolucionId == null || string.IsNullOrWhiteSpace(RemisionValor))
            {
                TempData["Error"] = "Datos incompletos para asignar remision.";
                return RedirectToPage();
            }

            await _salesService.AplicarRemisionDevolucionAsync(RemisionDevolucionId.Value, RemisionValor.Trim());
            TempData["Success"] = "Remision asignada exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al asignar remision");
            TempData["Error"] = $"Error al asignar remision: {ex.Message}";
        }

        return RedirectToPage();
    }
}
