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

    public async Task OnGetAsync(int? idRemision, string? fechaDesde)
    {
        IdRemision = idRemision;

        if (!string.IsNullOrEmpty(fechaDesde) && DateTime.TryParse(fechaDesde, out var fd))
            FechaDesde = fd;

        try
        {
            var remisionesTask = _service.ObtenerRemisionesAsync();
            var statsTask = _service.ObtenerEstadisticasAsync(IdRemision, FechaDesde);
            var enExistTask = _service.ObtenerEnExistenciaAsync(IdRemision, FechaDesde);
            var porDevTask = _service.ObtenerPorDevolverAsync(IdRemision, FechaDesde);
            var devueltasTask = _service.ObtenerDevueltasAsync(IdRemision, FechaDesde);

            await Task.WhenAll(remisionesTask, statsTask, enExistTask, porDevTask, devueltasTask);

            Remisiones = remisionesTask.Result;
            Stats = statsTask.Result;
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
