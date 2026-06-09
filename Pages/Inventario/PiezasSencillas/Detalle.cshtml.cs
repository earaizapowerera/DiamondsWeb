using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Inventario.PiezasSencillas;

[Authorize]
public class DetalleModel : PageModel
{
    private readonly InventoryService _inventoryService;
    private readonly CatalogService _catalogService;
    private readonly ILogger<DetalleModel> _logger;

    public DetalleModel(InventoryService inventoryService, CatalogService catalogService, ILogger<DetalleModel> logger)
    {
        _inventoryService = inventoryService;
        _catalogService = catalogService;
        _logger = logger;
    }

    [BindProperty]
    public Pieza Pieza { get; set; } = new();

    public bool EsNueva { get; set; } = true;

    public List<Proveedor> Proveedores { get; set; } = new();
    public List<Grupo> Grupos { get; set; } = new();
    public List<Moneda> Monedas { get; set; } = new();
    public List<Divisor> Divisores { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string? codigoBarras)
    {
        try
        {
            await CargarCatalogosAsync();

            if (!string.IsNullOrWhiteSpace(codigoBarras))
            {
                var pieza = await _inventoryService.ObtenerPiezaAsync(codigoBarras);
                if (pieza == null)
                {
                    TempData["Error"] = $"Pieza con codigo {codigoBarras} no encontrada.";
                    return RedirectToPage("Index");
                }
                Pieza = pieza;
                EsNueva = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar detalle de pieza {CB}", codigoBarras);
            TempData["Error"] = $"Error al cargar pieza: {ex.Message}";
            return RedirectToPage("Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? codigoBarras)
    {
        try
        {
            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid : 1;
            Pieza.IdUsuario = idUsuario;

            if (string.IsNullOrWhiteSpace(codigoBarras))
            {
                // Crear nueva pieza
                Pieza.IdStatus = 1;
                await _inventoryService.CrearPiezaSencillaAsync(Pieza);
                TempData["Success"] = $"Pieza {Pieza.CodigoBarras} creada exitosamente.";
                return RedirectToPage("Detalle", new { codigoBarras = Pieza.CodigoBarras });
            }
            else
            {
                // Actualizar pieza existente
                Pieza.CodigoBarras = codigoBarras;
                await _inventoryService.ActualizarPiezaSencillaAsync(Pieza);
                TempData["Success"] = "Pieza actualizada exitosamente.";
                return RedirectToPage("Detalle", new { codigoBarras });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar pieza");
            TempData["Error"] = $"Error al guardar pieza: {ex.Message}";
            await CargarCatalogosAsync();
            EsNueva = string.IsNullOrWhiteSpace(codigoBarras);
            return Page();
        }
    }

    private async Task CargarCatalogosAsync()
    {
        Proveedores = await _catalogService.ObtenerProveedoresAsync();
        Grupos = await _catalogService.ObtenerGruposAsync();
        Monedas = await _catalogService.ObtenerMonedasAsync();
        Divisores = await _catalogService.ObtenerDivisoresAsync();
    }
}
