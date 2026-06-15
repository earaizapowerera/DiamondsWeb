using DiamondsWeb.Extensions;
using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Inventario.Existencias;

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

    public List<RegistroInventarioFisico> Registros { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public bool SoloHoy { get; set; } = true;

    [BindProperty]
    public string CodigoBarras { get; set; } = "";

    public int TotalRegistros => Registros.Count;

    public async Task OnGetAsync()
    {
        try
        {
            Registros = await _inventoryService.ObtenerRegistroExistenciasAsync(SoloHoy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar registros de existencias");
            TempData["Error"] = $"Error al cargar registros: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostRegistrarAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(CodigoBarras))
            {
                TempData["Error"] = "El codigo de barras es requerido.";
                return RedirectToPage();
            }

            var idUsuario = User.GetRequiredIdUsuario();
            var resultado = await _inventoryService.RegistrarInventarioFisicoAsync(CodigoBarras.Trim(), idUsuario);

            if (resultado.Contains("no encontrada", StringComparison.OrdinalIgnoreCase))
                TempData["Error"] = resultado;
            else
                TempData["Success"] = resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar existencia");
            TempData["Error"] = $"Error al registrar: {ex.Message}";
        }

        return RedirectToPage();
    }
}
