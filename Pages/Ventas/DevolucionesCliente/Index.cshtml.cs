using DiamondsWeb.Extensions;
using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Ventas.DevolucionesCliente;

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

    public DevolucionCliente? Resultado { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? CodigoBarras { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(CodigoBarras))
            {
                Resultado = await _salesService.BuscarDevolucionClienteAsync(CodigoBarras.Trim());
                if (Resultado == null)
                    TempData["Error"] = "No se encontro informacion de compra para ese codigo de barras.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar devolucion de cliente");
            TempData["Error"] = $"Error al buscar: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostReestablecerAsync(string codigoBarras)
    {
        try
        {
            var idUsuario = User.GetRequiredIdUsuario();
            var resultado = await _salesService.ReestablecerPiezaAsync(codigoBarras, idUsuario);
            TempData["Success"] = resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reestablecer pieza {CodigoBarras}", codigoBarras);
            TempData["Error"] = $"Error al reestablecer pieza: {ex.Message}";
        }

        return RedirectToPage(new { CodigoBarras = codigoBarras });
    }
}
