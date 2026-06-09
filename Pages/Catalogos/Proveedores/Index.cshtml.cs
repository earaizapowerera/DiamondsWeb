using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Catalogos.Proveedores;

[Authorize]
public class IndexModel : PageModel
{
    private readonly CatalogService _catalogService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(CatalogService catalogService, ILogger<IndexModel> logger)
    {
        _catalogService = catalogService;
        _logger = logger;
    }

    public List<Proveedor> Proveedores { get; set; } = new();
    public List<DefaultUtilidad> Utilidades { get; set; } = new();
    public List<Moneda> Monedas { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    // ── Create fields ──
    [BindProperty] public string NuevoNombre { get; set; } = "";
    [BindProperty] public string? NuevoTelefono { get; set; }
    [BindProperty] public string? NuevaDireccion { get; set; }
    [BindProperty] public string? NuevoContacto { get; set; }
    [BindProperty] public int? NuevoIdDefaultCaracteristica { get; set; }
    [BindProperty] public int? NuevoIdDefaultTipoCosto { get; set; }
    [BindProperty] public int? NuevoIdDefaultUtilidad { get; set; }
    [BindProperty] public int? NuevoIdMoneda { get; set; }
    [BindProperty] public bool NuevoMonedaDefault { get; set; }
    [BindProperty] public bool NuevoUtilidadExtraPrecioGramo { get; set; }

    // ── Edit fields ──
    [BindProperty] public int? EditId { get; set; }
    [BindProperty] public string? EditNombre { get; set; }
    [BindProperty] public string? EditTelefono { get; set; }
    [BindProperty] public string? EditDireccion { get; set; }
    [BindProperty] public string? EditContacto { get; set; }
    [BindProperty] public int? EditIdDefaultCaracteristica { get; set; }
    [BindProperty] public int? EditIdDefaultTipoCosto { get; set; }
    [BindProperty] public int? EditIdDefaultUtilidad { get; set; }
    [BindProperty] public int? EditIdMoneda { get; set; }
    [BindProperty] public bool EditMonedaDefault { get; set; }
    [BindProperty] public bool EditUtilidadExtraPrecioGramo { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Proveedores = await _catalogService.ObtenerProveedoresAsync();
            Utilidades = await _catalogService.ObtenerDefaultsUtilidadAsync();
            Monedas = await _catalogService.ObtenerMonedasAsync();

            if (!string.IsNullOrWhiteSpace(Buscar))
                Proveedores = Proveedores
                    .Where(p => (p.NombreProveedor ?? "").Contains(Buscar, StringComparison.OrdinalIgnoreCase))
                    .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar proveedores");
            TempData["Error"] = $"Error al cargar proveedores: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NuevoNombre))
            {
                TempData["Error"] = "El nombre del proveedor es requerido.";
                return RedirectToPage();
            }

            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid : 1;
            var proveedor = new Proveedor
            {
                NombreProveedor = NuevoNombre.Trim(),
                Telefono = NuevoTelefono?.Trim(),
                Direccion = NuevaDireccion?.Trim(),
                Contacto = NuevoContacto?.Trim(),
                IdDefaultCaracteristica = NuevoIdDefaultCaracteristica,
                IdDefaultTipoCosto = NuevoIdDefaultTipoCosto,
                IdDefaultUtilidad = NuevoIdDefaultUtilidad,
                IdMoneda = NuevoIdMoneda,
                MonedaDefault = NuevoMonedaDefault,
                UtilidadExtraPrecioGramo = NuevoUtilidadExtraPrecioGramo,
                IdUsuario = idUsuario
            };
            await _catalogService.CrearProveedorAsync(proveedor);
            TempData["Success"] = "Proveedor creado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear proveedor");
            TempData["Error"] = $"Error al crear proveedor: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditAsync()
    {
        try
        {
            if (EditId == null || string.IsNullOrWhiteSpace(EditNombre))
            {
                TempData["Error"] = "Datos incompletos para editar.";
                return RedirectToPage();
            }

            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid : 1;
            var proveedor = new Proveedor
            {
                Proveedor1 = EditId.Value,
                NombreProveedor = EditNombre.Trim(),
                Telefono = EditTelefono?.Trim(),
                Direccion = EditDireccion?.Trim(),
                Contacto = EditContacto?.Trim(),
                IdDefaultCaracteristica = EditIdDefaultCaracteristica,
                IdDefaultTipoCosto = EditIdDefaultTipoCosto,
                IdDefaultUtilidad = EditIdDefaultUtilidad,
                IdMoneda = EditIdMoneda,
                MonedaDefault = EditMonedaDefault,
                UtilidadExtraPrecioGramo = EditUtilidadExtraPrecioGramo,
                IdUsuario = idUsuario
            };
            await _catalogService.ActualizarProveedorAsync(proveedor);
            TempData["Success"] = "Proveedor actualizado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar proveedor");
            TempData["Error"] = $"Error al actualizar proveedor: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            await _catalogService.EliminarProveedorAsync(id);
            TempData["Success"] = "Proveedor eliminado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar proveedor {Id}", id);
            TempData["Error"] = $"Error al eliminar proveedor: {ex.Message}";
        }

        return RedirectToPage();
    }
}
