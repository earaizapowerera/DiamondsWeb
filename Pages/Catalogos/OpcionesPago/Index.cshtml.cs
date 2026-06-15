using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Catalogos.OpcionesPago;

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

    public List<OpcionPago> OpcionesPago { get; set; } = new();
    public List<Moneda> Monedas { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    [BindProperty]
    public string NuevoNombre { get; set; } = "";

    [BindProperty]
    public int? NuevoIdMoneda { get; set; }

    [BindProperty]
    public int? NuevoIdLogo { get; set; }

    [BindProperty]
    public bool NuevoActivo { get; set; } = true;

    [BindProperty]
    public int? EditId { get; set; }

    [BindProperty]
    public string? EditNombre { get; set; }

    [BindProperty]
    public int? EditIdMoneda { get; set; }

    [BindProperty]
    public int? EditIdLogo { get; set; }

    [BindProperty]
    public bool EditActivo { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            OpcionesPago = await _catalogService.ObtenerOpcionesPagoAsync();
            Monedas = await _catalogService.ObtenerMonedasAsync();

            if (!string.IsNullOrWhiteSpace(Buscar))
                OpcionesPago = OpcionesPago
                    .Where(o => o.OpcionPago1.Contains(Buscar, StringComparison.OrdinalIgnoreCase))
                    .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar opciones de pago");
            TempData["Error"] = $"Error al cargar opciones de pago: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NuevoNombre))
            {
                TempData["Error"] = "El nombre de la opcion de pago es requerido.";
                return RedirectToPage();
            }

            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid
                : throw new UnauthorizedAccessException("IdUsuario claim not found");
            await _catalogService.CrearOpcionPagoAsync(NuevoNombre.Trim(), NuevoIdMoneda, NuevoIdLogo, NuevoActivo, idUsuario);
            TempData["Success"] = "Opcion de pago creada exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear opcion de pago");
            TempData["Error"] = $"Error al crear opcion de pago: {ex.Message}";
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

            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid
                : throw new UnauthorizedAccessException("IdUsuario claim not found");
            await _catalogService.ActualizarOpcionPagoAsync(EditId.Value, EditNombre.Trim(), EditIdMoneda, EditIdLogo, EditActivo, idUsuario);
            TempData["Success"] = "Opcion de pago actualizada exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar opcion de pago");
            TempData["Error"] = $"Error al actualizar opcion de pago: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            await _catalogService.EliminarOpcionPagoAsync(id);
            TempData["Success"] = "Opcion de pago eliminada exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar opcion de pago {Id}", id);
            TempData["Error"] = $"Error al eliminar opcion de pago: {ex.Message}";
        }

        return RedirectToPage();
    }
}
