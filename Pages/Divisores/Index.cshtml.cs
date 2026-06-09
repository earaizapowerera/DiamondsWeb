using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Divisores;

[Authorize]
public class IndexModel : PageModel
{
    private readonly DivisoresService _service;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(DivisoresService service, ILogger<IndexModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    public List<DivisorItem> Divisores { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    // Formulario crear/editar
    [BindProperty]
    public int? EditId { get; set; }

    [BindProperty]
    public string? Descripcion { get; set; }

    [BindProperty]
    public decimal Divisor { get; set; }

    public async Task OnGetAsync()
    {
        await CargarDivisoresAsync();
    }

    /// <summary>Crear nuevo divisor</summary>
    public async Task<IActionResult> OnPostCrearAsync()
    {
        if (string.IsNullOrWhiteSpace(Descripcion))
        {
            ErrorMessage = "La descripción es requerida.";
            await CargarDivisoresAsync();
            return Page();
        }

        if (Divisor <= 0)
        {
            ErrorMessage = "El divisor debe ser mayor a 0.";
            await CargarDivisoresAsync();
            return Page();
        }

        try
        {
            var id = await _service.CrearAsync(Descripcion.Trim(), Divisor, 1);
            SuccessMessage = $"Divisor #{id} creado correctamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear divisor");
            ErrorMessage = $"Error al crear: {ex.Message}";
        }

        await CargarDivisoresAsync();
        return Page();
    }

    /// <summary>Actualizar divisor existente</summary>
    public async Task<IActionResult> OnPostEditarAsync()
    {
        if (!EditId.HasValue || EditId <= 0)
        {
            ErrorMessage = "Id de divisor inválido.";
            await CargarDivisoresAsync();
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Descripcion))
        {
            ErrorMessage = "La descripción es requerida.";
            await CargarDivisoresAsync();
            return Page();
        }

        if (Divisor <= 0)
        {
            ErrorMessage = "El divisor debe ser mayor a 0.";
            await CargarDivisoresAsync();
            return Page();
        }

        try
        {
            var ok = await _service.ActualizarAsync(EditId.Value, Descripcion.Trim(), Divisor, 1);
            SuccessMessage = ok
                ? $"Divisor #{EditId.Value} actualizado correctamente."
                : "No se encontró el divisor a actualizar.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar divisor {Id}", EditId);
            ErrorMessage = $"Error al actualizar: {ex.Message}";
        }

        await CargarDivisoresAsync();
        return Page();
    }

    /// <summary>Eliminar divisor</summary>
    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        try
        {
            var ok = await _service.EliminarAsync(id);
            SuccessMessage = ok
                ? $"Divisor #{id} eliminado correctamente."
                : "No se encontró el divisor a eliminar.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar divisor {Id}", id);
            ErrorMessage = $"Error al eliminar: {ex.Message}";
        }

        await CargarDivisoresAsync();
        return Page();
    }

    private async Task CargarDivisoresAsync()
    {
        try
        {
            Divisores = await _service.ObtenerTodosAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar divisores");
            ErrorMessage = $"Error al cargar datos: {ex.Message}";
        }
    }
}
