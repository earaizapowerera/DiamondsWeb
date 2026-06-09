using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Proveedores;

[Authorize]
public class RazonesSocialesModel : PageModel
{
    private readonly ProveedorService _service;
    private readonly ILogger<RazonesSocialesModel> _logger;

    public RazonesSocialesModel(ProveedorService service, ILogger<RazonesSocialesModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    public List<RazonSocialProveedor> RazonesSociales { get; set; } = new();

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
            RazonesSociales = await _service.ObtenerRazonesSocialesAsync(Buscar);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cargando razones sociales");
            MensajeError = $"Error al consultar: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostGuardarAsync(
        int idRazonSocialProveedor, string razonSocial, string? rfc,
        string? calle, string? codigoPostal, string? colonia,
        string? municipio, string? estado)
    {
        if (string.IsNullOrWhiteSpace(razonSocial))
        {
            MensajeError = "La razón social es obligatoria.";
            return RedirectToPage();
        }

        var rs = new RazonSocialProveedor
        {
            IdRazonSocialProveedor = idRazonSocialProveedor,
            RazonSocialProveedorNombre = razonSocial.Trim(),
            RFC = rfc?.Trim(),
            Calle = calle?.Trim(),
            CodigoPostal = codigoPostal?.Trim(),
            Colonia = colonia?.Trim(),
            Municipio = municipio?.Trim(),
            Estado = estado?.Trim(),
            IdUsuario = 1 // usuario por defecto
        };

        try
        {
            if (idRazonSocialProveedor == 0)
            {
                var newId = await _service.CrearRazonSocialAsync(rs);
                MensajeExito = $"Razón social '{razonSocial}' creada (ID: {newId}).";
            }
            else
            {
                await _service.ActualizarRazonSocialAsync(rs);
                MensajeExito = $"Razón social '{razonSocial}' actualizada.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error guardando razón social");
            MensajeError = $"Error al guardar: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        try
        {
            var eliminada = await _service.EliminarRazonSocialAsync(id);
            if (eliminada)
                MensajeExito = $"Razón social #{id} eliminada.";
            else
                MensajeError = $"No se puede eliminar la razón social #{id} porque tiene proveedores asignados. Elimine las asignaciones primero.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando razón social {Id}", id);
            MensajeError = $"Error al eliminar: {ex.Message}";
        }

        return RedirectToPage();
    }
}
