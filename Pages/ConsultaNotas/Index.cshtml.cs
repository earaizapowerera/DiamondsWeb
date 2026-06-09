using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.ConsultaNotas;

[Authorize]
public class IndexModel : PageModel
{
    private readonly NotasService _notasService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(NotasService notasService, ILogger<IndexModel> logger)
    {
        _notasService = notasService;
        _logger = logger;
    }

    public List<NotaVenta> Notas { get; set; } = new();
    public decimal SumaNeto { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public bool BusquedaRealizada { get; set; }

    // Filtros principales (bind desde query string)
    [BindProperty(SupportsGet = true)] public DateTime? FechaDesde { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? FechaHasta { get; set; }
    [BindProperty(SupportsGet = true)] public string? NombreCliente { get; set; }

    // Filtros de pieza
    [BindProperty(SupportsGet = true)] public string? CodigoBarras { get; set; }
    [BindProperty(SupportsGet = true)] public string? Proveedor { get; set; }
    [BindProperty(SupportsGet = true)] public string? DescripcionPieza { get; set; }
    [BindProperty(SupportsGet = true)] public string? Grupo { get; set; }
    [BindProperty(SupportsGet = true)] public string? IdLocalizacion { get; set; }
    [BindProperty(SupportsGet = true)] public string? Modelo { get; set; }
    [BindProperty(SupportsGet = true)] public string? Serie { get; set; }
    [BindProperty(SupportsGet = true)] public decimal? PesoDesde { get; set; }
    [BindProperty(SupportsGet = true)] public decimal? PesoHasta { get; set; }
    [BindProperty(SupportsGet = true)] public decimal? QuilatesDesde { get; set; }
    [BindProperty(SupportsGet = true)] public decimal? QuilatesHasta { get; set; }

    public async Task OnGetAsync()
    {
        // Si hay al menos un filtro activo, ejecutar busqueda
        if (!TieneFiltros()) return;

        BusquedaRealizada = true;
        var filtro = BuildFiltro();

        try
        {
            Notas = await _notasService.BuscarNotasAsync(filtro);
            SumaNeto = await _notasService.ObtenerSumaNetoAsync(filtro);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error buscando notas");
            ErrorMessage = $"Error al consultar notas: {ex.Message}";
        }
    }

    /// <summary>
    /// Cancelar nota via POST (llama SP restaurarnota).
    /// </summary>
    public async Task<IActionResult> OnPostCancelarAsync(int idNota)
    {
        // TODO: Obtener IdUsuario real del sistema de auth
        var idUsuario = 1;

        var (success, message) = await _notasService.CancelarNotaAsync(idNota, idUsuario);

        if (success)
            TempData["SuccessMessage"] = message;
        else
            TempData["ErrorMessage"] = message;

        // Preservar filtros en el redirect
        return RedirectToPage(new
        {
            FechaDesde, FechaHasta, NombreCliente,
            CodigoBarras, Proveedor, DescripcionPieza,
            Grupo, IdLocalizacion, Modelo, Serie,
            PesoDesde, PesoHasta, QuilatesDesde, QuilatesHasta
        });
    }

    /// <summary>
    /// API endpoint: obtener detalle de una nota (piezas + pagos + totales).
    /// Se llama via AJAX al hacer click en una fila del grid.
    /// </summary>
    public async Task<IActionResult> OnGetDetalleAsync(int idNota)
    {
        try
        {
            var piezas = await _notasService.ObtenerPiezasNotaAsync(idNota);
            var pagos = await _notasService.ObtenerPagosNotaAsync(idNota);
            var totales = await _notasService.ObtenerTotalesCostoNetoAsync(idNota);

            return new JsonResult(new { piezas, pagos, totales });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo detalle de nota {IdNota}", idNota);
            return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }

    private bool TieneFiltros()
    {
        return FechaDesde.HasValue || FechaHasta.HasValue
            || !string.IsNullOrWhiteSpace(NombreCliente)
            || !string.IsNullOrWhiteSpace(CodigoBarras)
            || !string.IsNullOrWhiteSpace(Proveedor)
            || !string.IsNullOrWhiteSpace(DescripcionPieza)
            || !string.IsNullOrWhiteSpace(Grupo)
            || !string.IsNullOrWhiteSpace(IdLocalizacion)
            || !string.IsNullOrWhiteSpace(Modelo)
            || !string.IsNullOrWhiteSpace(Serie)
            || PesoDesde.HasValue || PesoHasta.HasValue
            || QuilatesDesde.HasValue || QuilatesHasta.HasValue;
    }

    private NotasFiltro BuildFiltro() => new()
    {
        FechaDesde = FechaDesde,
        FechaHasta = FechaHasta,
        NombreCliente = NombreCliente,
        CodigoBarras = CodigoBarras,
        Proveedor = Proveedor,
        DescripcionPieza = DescripcionPieza,
        Grupo = Grupo,
        IdLocalizacion = IdLocalizacion,
        Modelo = Modelo,
        Serie = Serie,
        PesoDesde = PesoDesde,
        PesoHasta = PesoHasta,
        QuilatesDesde = QuilatesDesde,
        QuilatesHasta = QuilatesHasta
    };
}
