using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Ventas.Consignacion;

[Authorize]
public class IndexModel : PageModel
{
    private readonly SalesService _salesService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(SalesService salesService, ILogger<IndexModel> logger)
    {
        _salesService = salesService;
        _logger = logger;
    }

    public List<ConsignacionItem> EnExistencia { get; set; } = new();
    public List<ConsignacionItem> PorDevolver { get; set; } = new();
    public List<ConsignacionItem> Devuelto { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? IdRemision { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            var todos = await _salesService.ObtenerConsignacionAsync(IdRemision);
            EnExistencia = todos.Where(c => c.Estado == "En Existencia").ToList();
            PorDevolver = todos.Where(c => c.Estado == "Por Devolver").ToList();
            Devuelto = todos.Where(c => c.Estado == "Devuelto").ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar consignacion");
            TempData["Error"] = $"Error al cargar consignacion: {ex.Message}";
        }
    }
}
