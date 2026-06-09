using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Inventario.Transferencias;

[Authorize]
public class IndexModel : PageModel
{
    private readonly InventoryService _inventoryService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(InventoryService inventoryService, ILogger<IndexModel> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    public List<Tienda> Tiendas { get; set; } = new();
    public List<Transferencia> Transferencias { get; set; } = new();

    [BindProperty]
    public string CodigoBarrasEnviar { get; set; } = "";

    [BindProperty]
    public int TiendaDestinoEnviar { get; set; }

    [BindProperty]
    public string CodigoBarrasRepetida { get; set; } = "";

    [BindProperty]
    public int CantidadRepetida { get; set; }

    [BindProperty]
    public int TiendaDestinoRepetida { get; set; }

    [BindProperty]
    public string CodigoBarrasRecibir { get; set; } = "";

    public async Task OnGetAsync()
    {
        try
        {
            Tiendas = await _inventoryService.ObtenerTiendasAsync();
            Transferencias = await _inventoryService.ObtenerTransferenciasAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar transferencias");
            TempData["Error"] = $"Error al cargar transferencias: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostEnviarAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(CodigoBarrasEnviar))
            {
                TempData["Error"] = "El codigo de barras es requerido.";
                return RedirectToPage();
            }

            if (TiendaDestinoEnviar <= 0)
            {
                TempData["Error"] = "Seleccione una tienda destino.";
                return RedirectToPage();
            }

            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid : 1;
            var resultado = await _inventoryService.TransferirPiezaAsync(CodigoBarrasEnviar.Trim(), TiendaDestinoEnviar, idUsuario);

            if (resultado.Contains("no encontrada", StringComparison.OrdinalIgnoreCase))
                TempData["Error"] = resultado;
            else
                TempData["Success"] = resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar pieza");
            TempData["Error"] = $"Error al enviar pieza: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEnviarRepetidaAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(CodigoBarrasRepetida))
            {
                TempData["Error"] = "El codigo de barras es requerido.";
                return RedirectToPage();
            }

            if (TiendaDestinoRepetida <= 0)
            {
                TempData["Error"] = "Seleccione una tienda destino.";
                return RedirectToPage();
            }

            if (CantidadRepetida <= 0)
            {
                TempData["Error"] = "La cantidad debe ser mayor a 0.";
                return RedirectToPage();
            }

            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid : 1;
            var errores = new List<string>();
            var exitos = 0;

            for (int i = 0; i < CantidadRepetida; i++)
            {
                var resultado = await _inventoryService.TransferirPiezaAsync(CodigoBarrasRepetida.Trim(), TiendaDestinoRepetida, idUsuario);
                if (resultado.Contains("no encontrada", StringComparison.OrdinalIgnoreCase))
                    errores.Add(resultado);
                else
                    exitos++;
            }

            if (exitos > 0)
                TempData["Success"] = $"{exitos} transferencia(s) completada(s).";
            if (errores.Any())
                TempData["Error"] = string.Join("; ", errores.Distinct());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar repetidas");
            TempData["Error"] = $"Error al enviar repetidas: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRecibirAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(CodigoBarrasRecibir))
            {
                TempData["Error"] = "El codigo de barras es requerido.";
                return RedirectToPage();
            }

            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid : 1;
            // Recibir = transferir a la tienda del usuario actual
            var idTienda = int.TryParse(User.FindFirst("IdTienda")?.Value, out var tid) ? tid : 1;
            var resultado = await _inventoryService.TransferirPiezaAsync(CodigoBarrasRecibir.Trim(), idTienda, idUsuario);

            if (resultado.Contains("no encontrada", StringComparison.OrdinalIgnoreCase))
                TempData["Error"] = resultado;
            else
                TempData["Success"] = $"Pieza recibida. {resultado}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al recibir pieza");
            TempData["Error"] = $"Error al recibir pieza: {ex.Message}";
        }

        return RedirectToPage();
    }
}
