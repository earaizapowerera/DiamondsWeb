using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Catalogo;

[Authorize]
public class RepetidasModel : PageModel
{
    private readonly CatalogoRepetidasService _service;
    private readonly ILogger<RepetidasModel> _logger;

    public RepetidasModel(CatalogoRepetidasService service, ILogger<RepetidasModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    // --- Datos para la vista ---
    public List<RepetidaItem> Items { get; set; } = new();
    public List<CatalogoDropdownItem> Proveedores { get; set; } = new();
    public List<CatalogoDropdownItem> Grupos { get; set; } = new();
    public List<DivisorDropdownItem> Divisores { get; set; } = new();
    public RepetidaItem? ItemEditar { get; set; }
    public string? MensajeExito { get; set; }
    public string? MensajeError { get; set; }

    // --- Parámetros de búsqueda ---
    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    /// <summary>
    /// Si viene este parámetro, se abre el modal en modo edición
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? Editar { get; set; }

    /// <summary>
    /// Si viene este flag, se abre el modal en modo nuevo
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public bool Nuevo { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Items = await _service.ListarAsync(Buscar);
            await CargarDropdownsAsync();

            if (!string.IsNullOrEmpty(Editar))
            {
                ItemEditar = await _service.ObtenerPorCodigoAsync(Editar);
            }

            // Leer mensajes de TempData
            MensajeExito = TempData["Exito"]?.ToString();
            MensajeError = TempData["Error"]?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cargando catálogo de repetidas");
            MensajeError = $"Error al consultar: {ex.Message}";
        }
    }

    /// <summary>
    /// Crear nueva pieza repetida con código de barras auto-generado
    /// </summary>
    public async Task<IActionResult> OnPostCrearAsync(
        string descripcion, int proveedor, int idGrupo,
        short? kilates, int? precio, int idDivisor)
    {
        try
        {
            var form = new RepetidaForm
            {
                Descripcion = descripcion,
                Proveedor = proveedor,
                IdGrupo = idGrupo,
                Kilates = kilates,
                Precio = precio,
                IdDivisor = idDivisor
            };

            var codigo = await _service.CrearAsync(form);
            TempData["Exito"] = $"Pieza creada con codigo de barras: {codigo}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creando pieza repetida");
            TempData["Error"] = $"Error al crear: {ex.Message}";
        }

        return RedirectToPage(new { Buscar });
    }

    /// <summary>
    /// Actualizar pieza existente
    /// </summary>
    public async Task<IActionResult> OnPostEditarAsync(
        string codigoBarras, string descripcion, int proveedor,
        int idGrupo, short? kilates, int? precio, int idDivisor)
    {
        try
        {
            var form = new RepetidaForm
            {
                CodigoBarras = codigoBarras,
                Descripcion = descripcion,
                Proveedor = proveedor,
                IdGrupo = idGrupo,
                Kilates = kilates,
                Precio = precio,
                IdDivisor = idDivisor
            };

            await _service.ActualizarAsync(form);
            TempData["Exito"] = $"Pieza {codigoBarras} actualizada correctamente";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando pieza {Codigo}", codigoBarras);
            TempData["Error"] = $"Error al actualizar: {ex.Message}";
        }

        return RedirectToPage(new { Buscar });
    }

    /// <summary>
    /// Eliminar pieza
    /// </summary>
    public async Task<IActionResult> OnPostEliminarAsync(string codigoBarras)
    {
        try
        {
            await _service.EliminarAsync(codigoBarras);
            TempData["Exito"] = $"Pieza {codigoBarras} eliminada";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando pieza {Codigo}", codigoBarras);
            TempData["Error"] = $"Error al eliminar: {ex.Message}";
        }

        return RedirectToPage(new { Buscar });
    }

    private async Task CargarDropdownsAsync()
    {
        Proveedores = await _service.ObtenerTodosProveedoresAsync();
        Grupos = await _service.ObtenerGruposAsync();
        Divisores = await _service.ObtenerDivisoresAsync();
    }
}
