using DiamondsWeb.Extensions;
using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Catalogos.RazonesSociales;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ProveedorService _proveedorService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ProveedorService proveedorService, ILogger<IndexModel> logger)
    {
        _proveedorService = proveedorService;
        _logger = logger;
    }

    public List<RazonSocialProveedor> RazonesSociales { get; set; } = new();
    public List<ProveedorSimple> Proveedores { get; set; } = new();
    public List<RazonSocialProveedorAsignacion> Asignaciones { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? VerAsignaciones { get; set; }

    // ── Create fields ──
    [BindProperty] public string NuevoRazonSocial { get; set; } = "";
    [BindProperty] public string? NuevoRFC { get; set; }
    [BindProperty] public string? NuevoCalle { get; set; }
    [BindProperty] public string? NuevoColonia { get; set; }
    [BindProperty] public string? NuevoCP { get; set; }
    [BindProperty] public string? NuevoMunicipio { get; set; }
    [BindProperty] public string? NuevoEstado { get; set; }

    // ── Edit fields ──
    [BindProperty] public int? EditId { get; set; }
    [BindProperty] public string? EditRazonSocial { get; set; }
    [BindProperty] public string? EditRFC { get; set; }
    [BindProperty] public string? EditCalle { get; set; }
    [BindProperty] public string? EditColonia { get; set; }
    [BindProperty] public string? EditCP { get; set; }
    [BindProperty] public string? EditMunicipio { get; set; }
    [BindProperty] public string? EditEstado { get; set; }

    // ── Asignacion fields ──
    [BindProperty] public int AsignarIdRS { get; set; }
    [BindProperty] public int AsignarProveedor { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            RazonesSociales = await _proveedorService.ObtenerRazonesSocialesAsync(Buscar);
            Proveedores = await _proveedorService.ObtenerProveedoresAsync();

            if (VerAsignaciones.HasValue)
                Asignaciones = await _proveedorService.ObtenerAsignacionesAsync(VerAsignaciones.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar razones sociales");
            TempData["Error"] = $"Error al cargar razones sociales: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NuevoRazonSocial))
            {
                TempData["Error"] = "La razon social es requerida.";
                return RedirectToPage();
            }

            var idUsuario = User.GetRequiredIdUsuario();
            var rs = new RazonSocialProveedor
            {
                RazonSocialProveedorNombre = NuevoRazonSocial.Trim(),
                RFC = NuevoRFC?.Trim(),
                Calle = NuevoCalle?.Trim(),
                Colonia = NuevoColonia?.Trim(),
                CodigoPostal = NuevoCP?.Trim(),
                Municipio = NuevoMunicipio?.Trim(),
                Estado = NuevoEstado?.Trim(),
                IdUsuario = idUsuario
            };
            await _proveedorService.CrearRazonSocialAsync(rs);
            TempData["Success"] = "Razon social creada exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear razon social");
            TempData["Error"] = $"Error al crear razon social: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditAsync()
    {
        try
        {
            if (EditId == null || string.IsNullOrWhiteSpace(EditRazonSocial))
            {
                TempData["Error"] = "Datos incompletos para editar.";
                return RedirectToPage();
            }

            var idUsuario = User.GetRequiredIdUsuario();
            var rs = new RazonSocialProveedor
            {
                IdRazonSocialProveedor = EditId.Value,
                RazonSocialProveedorNombre = EditRazonSocial.Trim(),
                RFC = EditRFC?.Trim(),
                Calle = EditCalle?.Trim(),
                Colonia = EditColonia?.Trim(),
                CodigoPostal = EditCP?.Trim(),
                Municipio = EditMunicipio?.Trim(),
                Estado = EditEstado?.Trim(),
                IdUsuario = idUsuario
            };
            await _proveedorService.ActualizarRazonSocialAsync(rs);
            TempData["Success"] = "Razon social actualizada exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar razon social");
            TempData["Error"] = $"Error al actualizar razon social: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            await _proveedorService.EliminarRazonSocialAsync(id);
            TempData["Success"] = "Razon social eliminada exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar razon social {Id}", id);
            TempData["Error"] = $"Error al eliminar razon social: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAsignarAsync()
    {
        try
        {
            var idUsuario = User.GetRequiredIdUsuario();
            await _proveedorService.CrearAsignacionAsync(AsignarIdRS, AsignarProveedor, idUsuario);
            TempData["Success"] = "Proveedor asignado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al asignar proveedor");
            TempData["Error"] = $"Error al asignar proveedor: {ex.Message}";
        }

        return RedirectToPage(new { VerAsignaciones = AsignarIdRS });
    }

    public async Task<IActionResult> OnPostDesasignarAsync(int idRS, int proveedor)
    {
        try
        {
            await _proveedorService.EliminarAsignacionAsync(idRS, proveedor);
            TempData["Success"] = "Proveedor desasignado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desasignar proveedor");
            TempData["Error"] = $"Error al desasignar proveedor: {ex.Message}";
        }

        return RedirectToPage(new { VerAsignaciones = idRS });
    }
}
