using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.TiposCambio;

[Authorize]
public class IndexModel : PageModel
{
    private readonly TiposCambioService _service;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(TiposCambioService service, ILogger<IndexModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    // --- Data ---
    public List<TipoCambioItem> TiposCambio { get; set; } = new();
    public List<TipoCambioVigente> Vigentes { get; set; } = new();
    public List<MonedaItem> Monedas { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    // --- Filtro ---
    [BindProperty(SupportsGet = true)]
    public int? FiltroMoneda { get; set; }

    // --- Formulario crear ---
    [BindProperty]
    public int IdMoneda { get; set; }

    [BindProperty]
    public decimal TipoCambioCotizacion { get; set; }

    [BindProperty]
    public decimal? TipoCambioVenta { get; set; }

    // --- Formulario editar ---
    [BindProperty]
    public int EditId { get; set; }

    [BindProperty]
    public decimal EditCotizacion { get; set; }

    [BindProperty]
    public decimal? EditVenta { get; set; }

    public async Task OnGetAsync()
    {
        await CargarDatosAsync();
    }

    /// <summary>
    /// Registrar nuevo tipo de cambio
    /// </summary>
    public async Task<IActionResult> OnPostCrearAsync()
    {
        try
        {
            if (TipoCambioCotizacion <= 0)
            {
                ErrorMessage = "El tipo de cambio cotizacion debe ser mayor a 0.";
                await CargarDatosAsync();
                return Page();
            }

            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid
                : throw new UnauthorizedAccessException("IdUsuario claim not found");
            await _service.CreateAsync(IdMoneda, TipoCambioCotizacion, TipoCambioVenta, idUsuario);
            SuccessMessage = "Tipo de cambio registrado correctamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear tipo de cambio");
            ErrorMessage = $"Error al registrar: {ex.Message}";
        }

        await CargarDatosAsync();
        return Page();
    }

    /// <summary>
    /// Editar tipo de cambio existente
    /// </summary>
    public async Task<IActionResult> OnPostEditarAsync()
    {
        try
        {
            if (EditCotizacion <= 0)
            {
                ErrorMessage = "El tipo de cambio cotizacion debe ser mayor a 0.";
                await CargarDatosAsync();
                return Page();
            }

            var ok = await _service.UpdateAsync(EditId, EditCotizacion, EditVenta);
            SuccessMessage = ok
                ? "Tipo de cambio actualizado correctamente."
                : "No se encontro el registro a actualizar.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al editar tipo de cambio {Id}", EditId);
            ErrorMessage = $"Error al actualizar: {ex.Message}";
        }

        await CargarDatosAsync();
        return Page();
    }

    /// <summary>
    /// Eliminar tipo de cambio
    /// </summary>
    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        try
        {
            var ok = await _service.DeleteAsync(id);
            SuccessMessage = ok
                ? "Tipo de cambio eliminado."
                : "No se encontro el registro a eliminar.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar tipo de cambio {Id}", id);
            ErrorMessage = $"Error al eliminar: {ex.Message}";
        }

        await CargarDatosAsync();
        return Page();
    }

    private async Task CargarDatosAsync()
    {
        try
        {
            Monedas = await _service.GetMonedasAsync();
            Vigentes = await _service.GetVigentesAsync();
            TiposCambio = await _service.GetAllAsync(FiltroMoneda);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cargando datos de tipos de cambio");
            ErrorMessage = $"Error al consultar datos: {ex.Message}";
        }
    }
}
