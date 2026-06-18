using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Inventario.VisorFotografias;

/// <summary>
/// Visor de Fotografías / CBO — Control de visibilidad de piezas en catálogo fotográfico.
/// Origen VB6: frmOcultar.frm (frmCBO) en Consultas2.vbp.
/// </summary>
[Authorize]
public class IndexModel : PageModel
{
    private readonly VisorFotografiasService _service;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(VisorFotografiasService service, ILogger<IndexModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    public List<PiezaCbo> Piezas { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    public int TotalPiezas => Piezas.Count;
    public int TotalVisibles => Piezas.Count(p => p.Visible == 1);

    public async Task OnGetAsync()
    {
        try
        {
            Piezas = await _service.ObtenerPiezasAsync(Buscar);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar visor de fotografías");
            TempData["Error"] = $"Error al cargar piezas: {ex.Message}";
        }
    }

    /// <summary>
    /// Guarda los cambios de visibilidad individuales desde el grid.
    /// Recibe la lista completa de códigos visibles vía AJAX.
    /// </summary>
    public async Task<IActionResult> OnPostGuardarAsync([FromBody] GuardarRequest request)
    {
        try
        {
            var codigosVisibles = request.CodigosVisibles ?? new List<string>();
            var count = await _service.GuardarVisibilidadAsync(codigosVisibles);
            return new JsonResult(new { success = true, message = $"{count} pieza(s) marcadas como visibles." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar visibilidad CBO");
            return new JsonResult(new { success = false, message = $"Error al guardar: {ex.Message}" });
        }
    }

    /// <summary>
    /// Establece 1 o 0 a todos los registros del filtro actual.
    /// </summary>
    public async Task<IActionResult> OnPostEstablecerTodosAsync([FromBody] EstablecerTodosRequest request)
    {
        try
        {
            var count = await _service.EstablecerTodosAsync(request.Visible, request.Buscar);
            var accion = request.Visible ? "visibles" : "ocultas";
            return new JsonResult(new { success = true, message = $"{count} pieza(s) marcadas como {accion}." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en operación masiva CBO");
            return new JsonResult(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    public class GuardarRequest
    {
        public List<string> CodigosVisibles { get; set; } = new();
    }

    public class EstablecerTodosRequest
    {
        public bool Visible { get; set; }
        public string? Buscar { get; set; }
    }
}
