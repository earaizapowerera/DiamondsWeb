using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Inventario.PiezasCompuestas;

[Authorize]
public class IndexModel : PageModel
{
    private readonly InventoryService _inventoryService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(InventoryService inventoryService, ILogger<IndexModel> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    public List<PiezaCompuesta> Piezas { get; set; } = new();
    public Dictionary<string, List<ComponenteCompuesta>> ComponentesPorPieza { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    // Campos para crear nueva pieza compuesta
    [BindProperty]
    public string? NuevaDescripcion { get; set; }

    [BindProperty]
    public string? NuevaEtiquetaK { get; set; }

    [BindProperty]
    public string? NuevaLinea1 { get; set; }

    [BindProperty]
    public string? NuevaLinea2 { get; set; }

    [BindProperty]
    public string? NuevaLinea3 { get; set; }

    // Campos para agregar/remover componente
    [BindProperty]
    public string? CBPadre { get; set; }

    [BindProperty]
    public string? CBComponente { get; set; }

    [BindProperty]
    public int IndiceComponente { get; set; }

    // Expandir detalle
    [BindProperty(SupportsGet = true)]
    public string? Expandir { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Piezas = await _inventoryService.ObtenerPiezasCompuestasAsync(Buscar);

            // Si hay una pieza expandida, cargar sus componentes
            if (!string.IsNullOrWhiteSpace(Expandir))
            {
                var componentes = await _inventoryService.ObtenerComponentesAsync(Expandir);
                ComponentesPorPieza[Expandir] = componentes;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar piezas compuestas");
            TempData["Error"] = $"Error al cargar piezas compuestas: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NuevaDescripcion))
            {
                TempData["Error"] = "La descripcion es requerida.";
                return RedirectToPage();
            }

            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid
                : throw new UnauthorizedAccessException("IdUsuario claim not found");
            var pc = new PiezaCompuesta
            {
                Descripcion = NuevaDescripcion.Trim(),
                EtiquetaK = NuevaEtiquetaK?.Trim(),
                Linea1 = NuevaLinea1?.Trim(),
                Linea2 = NuevaLinea2?.Trim(),
                Linea3 = NuevaLinea3?.Trim(),
                Componentes = 0,
                IdUsuario = idUsuario
            };

            var cb = await _inventoryService.CrearPiezaCompuestaAsync(pc);
            TempData["Success"] = $"Pieza compuesta {cb} creada exitosamente.";
            return RedirectToPage(new { Expandir = cb });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear pieza compuesta");
            TempData["Error"] = $"Error al crear pieza compuesta: {ex.Message}";
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostAgregarComponenteAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(CBPadre) || string.IsNullOrWhiteSpace(CBComponente))
            {
                TempData["Error"] = "El codigo del componente es requerido.";
                return RedirectToPage(new { Expandir = CBPadre });
            }

            await _inventoryService.AgregarComponenteAsync(CBPadre, CBComponente.Trim(), IndiceComponente);
            TempData["Success"] = $"Componente {CBComponente} agregado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al agregar componente");
            TempData["Error"] = $"Error al agregar componente: {ex.Message}";
        }

        return RedirectToPage(new { Expandir = CBPadre });
    }

    public async Task<IActionResult> OnPostRemoverComponenteAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(CBPadre) || string.IsNullOrWhiteSpace(CBComponente))
            {
                TempData["Error"] = "Datos incompletos para remover componente.";
                return RedirectToPage(new { Expandir = CBPadre });
            }

            await _inventoryService.RemoverComponenteAsync(CBPadre, CBComponente);
            TempData["Success"] = $"Componente {CBComponente} removido exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al remover componente");
            TempData["Error"] = $"Error al remover componente: {ex.Message}";
        }

        return RedirectToPage(new { Expandir = CBPadre });
    }
}
