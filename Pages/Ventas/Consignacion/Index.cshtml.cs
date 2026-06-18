using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Ventas.Consignacion;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ConsignacionService _service;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ConsignacionService service, ILogger<IndexModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    public List<PiezaConsignacion> EnExistencia { get; set; } = new();
    public List<PiezaConsignacion> PorDevolver { get; set; } = new();
    public List<PiezaConsignacion> Devueltas { get; set; } = new();
    public ConsignacionStats Stats { get; set; } = new();
    public List<RemisionConsignacionResumen> Remisiones { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public int? IdRemision { get; set; }
    public DateTime? FechaDesde { get; set; }

    // Paginación por grid
    public int PageExistencia { get; set; } = 1;
    public int PagePorDevolver { get; set; } = 1;
    public int PageDevueltas { get; set; } = 1;
    public int PageSize => ConsignacionService.PageSize;

    public int TotalPagesExistencia =>
        (int)Math.Ceiling((double)Stats.PiezasEnExistencia / PageSize);
    public int TotalPagesPorDevolver =>
        (int)Math.Ceiling((double)Stats.PiezasPorDevolver / PageSize);
    public int TotalPagesDevueltas =>
        (int)Math.Ceiling((double)Stats.PiezasDevueltas / PageSize);

    public async Task OnGetAsync(int? idRemision, string? fechaDesde,
        int pageExistencia = 1, int pagePorDevolver = 1, int pageDevueltas = 1)
    {
        IdRemision = idRemision;
        PageExistencia = Math.Max(pageExistencia, 1);
        PagePorDevolver = Math.Max(pagePorDevolver, 1);
        PageDevueltas = Math.Max(pageDevueltas, 1);

        if (!string.IsNullOrEmpty(fechaDesde) && DateTime.TryParse(fechaDesde, out var fd))
            FechaDesde = fd;

        try
        {
            // Stats y remisiones primero
            var remisionesTask = _service.ObtenerRemisionesAsync();
            var statsTask = _service.ObtenerEstadisticasAsync(IdRemision, FechaDesde);

            await Task.WhenAll(remisionesTask, statsTask);

            Remisiones = remisionesTask.Result;
            Stats = statsTask.Result;

            // Grids paginados en paralelo
            var enExistTask = _service.ObtenerEnExistenciaAsync(
                IdRemision, FechaDesde, PageExistencia);
            var porDevTask = _service.ObtenerPorDevolverAsync(
                IdRemision, FechaDesde, PagePorDevolver);
            var devueltasTask = _service.ObtenerDevueltasAsync(
                IdRemision, FechaDesde, PageDevueltas);

            await Task.WhenAll(enExistTask, porDevTask, devueltasTask);

            EnExistencia = enExistTask.Result;
            PorDevolver = porDevTask.Result;
            Devueltas = devueltasTask.Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cargando datos de consignacion");
            ErrorMessage = $"Error al consultar datos: {ex.Message}";
        }
    }
}
