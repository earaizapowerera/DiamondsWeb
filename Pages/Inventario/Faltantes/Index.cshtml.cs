using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Inventario.Faltantes;

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

    public List<PiezaFaltante> Faltantes { get; set; } = new();

    [BindProperty]
    public string? ComentarioCB { get; set; }

    [BindProperty]
    public string? ComentarioTexto { get; set; }

    public int TotalFaltantes => Faltantes.Count;

    public decimal SumaPrecios => Faltantes.Sum(f => f.Precio ?? 0);

    public async Task OnGetAsync()
    {
        try
        {
            Faltantes = await _inventoryService.ObtenerFaltantesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar faltantes");
            TempData["Error"] = $"Error al cargar faltantes: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostComentarioAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ComentarioCB))
            {
                TempData["Error"] = "Codigo de barras requerido.";
                return RedirectToPage();
            }

            await _inventoryService.GuardarComentarioFaltanteAsync(ComentarioCB.Trim(), ComentarioTexto?.Trim() ?? "");
            TempData["Success"] = "Comentario guardado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar comentario para {CB}", ComentarioCB);
            TempData["Error"] = $"Error al guardar comentario: {ex.Message}";
        }

        return RedirectToPage();
    }
}
