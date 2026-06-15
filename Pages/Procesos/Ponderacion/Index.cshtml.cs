using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Procesos.Ponderacion;

[Authorize]
public class IndexModel : PageModel
{
    private readonly PonderacionService _ponderacionService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(PonderacionService ponderacionService, ILogger<IndexModel> logger)
    {
        _ponderacionService = ponderacionService;
        _logger = logger;
    }

    // --- Propiedades de búsqueda ---
    [BindProperty(SupportsGet = true)]
    public int? IdRemision { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? IdFactura { get; set; }

    // --- Propiedades del formulario de ponderación ---
    [BindProperty]
    public string? Concepto { get; set; }

    [BindProperty]
    public decimal? Porcentaje { get; set; }

    [BindProperty]
    public bool ModificarTodos { get; set; }

    // --- Datos para la vista ---
    public int PiezasAfectadas { get; set; }
    public List<PiezaPonderacionPreview> PiezasPreview { get; set; } = new();
    public bool MostrarPreview { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            if (!IdRemision.HasValue && !IdFactura.HasValue)
                return;

            // Exclusividad: si vienen ambos, priorizar remisión
            if (IdRemision.HasValue)
                IdFactura = null;

            bool soloSinCosto = !ModificarTodos;

            PiezasAfectadas = await _ponderacionService.ContarPiezasAfectadasAsync(
                IdRemision, IdFactura, soloSinCosto);

            if (PiezasAfectadas > 0)
            {
                PiezasPreview = await _ponderacionService.ObtenerPreviewAsync(
                    IdRemision, IdFactura, soloSinCosto);
                MostrarPreview = true;
            }
            else
            {
                TempData["Error"] = "No se encontraron piezas con los criterios especificados.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar piezas para ponderación");
            TempData["Error"] = $"Error al buscar piezas: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostPonderarAsync()
    {
        try
        {
            // Validaciones
            if (!IdRemision.HasValue && !IdFactura.HasValue)
            {
                TempData["Error"] = "Debe especificar un Id de Remisión o Id de Factura.";
                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(Concepto))
            {
                TempData["Error"] = "Debe ingresar un concepto para el costo extra.";
                return RedirectToPage(new { IdRemision, IdFactura });
            }

            if (!Porcentaje.HasValue || Porcentaje.Value <= 0)
            {
                TempData["Error"] = "Debe ingresar un porcentaje válido (mayor a 0).";
                return RedirectToPage(new { IdRemision, IdFactura });
            }

            // Exclusividad
            if (IdRemision.HasValue)
                IdFactura = null;

            bool soloSinCosto = !ModificarTodos;

            var afectadas = await _ponderacionService.EjecutarPonderacionAsync(
                IdRemision, IdFactura, Porcentaje.Value, Concepto.Trim(), soloSinCosto);

            if (afectadas > 0)
            {
                var tipo = IdRemision.HasValue ? "remisión" : "factura";
                var id = IdRemision ?? IdFactura!.Value;
                TempData["Success"] = $"Ponderación aplicada: {afectadas} piezas actualizadas en {tipo} {id} con {Porcentaje.Value}% de costo extra ({Concepto.Trim()}).";
            }
            else
            {
                TempData["Error"] = "No se encontraron piezas para actualizar con los criterios especificados.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al ejecutar ponderación");
            TempData["Error"] = $"Error al ejecutar ponderación: {ex.Message}";
        }

        return RedirectToPage(new { IdRemision, IdFactura });
    }
}
