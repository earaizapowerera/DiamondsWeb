using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Proveedores;

[Authorize]
public class AsignacionesModel : PageModel
{
    private readonly ProveedorService _service;
    private readonly ILogger<AsignacionesModel> _logger;

    public AsignacionesModel(ProveedorService service, ILogger<AsignacionesModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    public List<RazonSocialProveedorAsignacion> Asignaciones { get; set; } = new();
    public List<ProveedorSimple> ProveedoresCombo { get; set; } = new();
    public List<RazonSocialProveedor> RazonesSocialesCombo { get; set; } = new();
    public RazonSocialProveedor? RazonSocialSeleccionada { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? IdRazonSocial { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    [TempData]
    public string? MensajeExito { get; set; }

    [TempData]
    public string? MensajeError { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            if (IdRazonSocial.HasValue)
                RazonSocialSeleccionada = await _service.ObtenerRazonSocialPorIdAsync(IdRazonSocial.Value);

            Asignaciones = await _service.ObtenerAsignacionesAsync(IdRazonSocial, Buscar);
            ProveedoresCombo = await _service.ObtenerProveedoresAsync();

            if (!IdRazonSocial.HasValue)
                RazonesSocialesCombo = await _service.ObtenerRazonesSocialesParaComboAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cargando asignaciones");
            MensajeError = $"Error al consultar: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostCrearAsync(int idRazonSocialForm, int proveedorId, int? idRazonSocial)
    {
        if (idRazonSocialForm <= 0 || proveedorId <= 0)
        {
            MensajeError = "Debe seleccionar razón social y proveedor.";
            return RedirectToPage(new { idRazonSocial });
        }

        try
        {
            var result = await _service.CrearAsignacionAsync(idRazonSocialForm, proveedorId, 1);
            if (result == -1)
                MensajeError = "Esta asignación ya existe.";
            else
                MensajeExito = $"Asignación creada (ID: {result}).";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creando asignación RS={RS} Prov={Prov}", idRazonSocialForm, proveedorId);
            MensajeError = $"Error al asignar: {ex.Message}";
        }

        return RedirectToPage(new { idRazonSocial });
    }

    public async Task<IActionResult> OnPostEliminarAsync(int id, int? idRazonSocial)
    {
        try
        {
            await _service.EliminarAsignacionAsync(id);
            MensajeExito = $"Asignación #{id} eliminada.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando asignación {Id}", id);
            MensajeError = $"Error al eliminar: {ex.Message}";
        }

        return RedirectToPage(new { idRazonSocial });
    }
}
