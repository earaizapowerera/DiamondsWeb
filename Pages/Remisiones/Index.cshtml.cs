using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Remisiones;

[Authorize]
public class IndexModel : PageModel
{
    private readonly RemisionService _service;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(RemisionService service, ILogger<IndexModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    // --- Listado de remisiones ---
    public List<RemisionResumen> Remisiones { get; set; } = new();
    public List<ProveedorItem> Proveedores { get; set; } = new();

    // --- Remision seleccionada ---
    public RemisionResumen? RemisionActual { get; set; }
    public List<PiezaDisponible> PiezasDisponibles { get; set; } = new();
    public List<PiezaRemision> PiezasRemision { get; set; } = new();
    public RemisionTotales Totales { get; set; } = new();

    // --- Filtros ---
    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? FiltroProveedor { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? SoloConsignacion { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? SelId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? BuscarPieza { get; set; }

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        await CargarDatosAsync();
    }

    /// <summary>
    /// Crear nueva remision.
    /// </summary>
    public async Task<IActionResult> OnPostCrearAsync(
        int proveedor, string remision, DateTime? fechaRemision, bool consignacion)
    {
        try
        {
            var idRemision = await _service.CrearRemisionAsync(
                proveedor, remision, fechaRemision, consignacion, idUsuario: 1);

            SuccessMessage = $"Remision #{idRemision} creada exitosamente.";
            SelId = idRemision;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creando remision");
            ErrorMessage = $"Error al crear remision: {ex.Message}";
        }

        await CargarDatosAsync();
        return Page();
    }

    /// <summary>
    /// Actualizar remision existente.
    /// </summary>
    public async Task<IActionResult> OnPostEditarAsync(
        int idRemision, int proveedor, string remision,
        DateTime? fechaRemision, bool consignacion)
    {
        try
        {
            await _service.ActualizarRemisionAsync(
                idRemision, proveedor, remision, fechaRemision, consignacion);

            SuccessMessage = $"Remision #{idRemision} actualizada.";
            SelId = idRemision;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando remision {Id}", idRemision);
            ErrorMessage = $"Error al actualizar: {ex.Message}";
        }

        await CargarDatosAsync();
        return Page();
    }

    /// <summary>
    /// Eliminar remision (solo si no tiene piezas).
    /// </summary>
    public async Task<IActionResult> OnPostEliminarAsync(int idRemision)
    {
        try
        {
            var ok = await _service.EliminarRemisionAsync(idRemision);
            if (ok)
            {
                SuccessMessage = $"Remision #{idRemision} eliminada.";
                SelId = null;
            }
            else
            {
                ErrorMessage = "No se puede eliminar: la remision tiene piezas vinculadas. Desvinculelas primero.";
                SelId = idRemision;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando remision {Id}", idRemision);
            ErrorMessage = $"Error al eliminar: {ex.Message}";
        }

        await CargarDatosAsync();
        return Page();
    }

    /// <summary>
    /// Vincular pieza individual a la remision seleccionada.
    /// </summary>
    public async Task<IActionResult> OnPostVincularAsync(
        int idRemision, string codigoBarras)
    {
        try
        {
            await _service.VincularPiezaAsync(idRemision, codigoBarras);
            SuccessMessage = $"Pieza {codigoBarras} vinculada a remision #{idRemision}.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error vinculando pieza {CB} a remision {Id}", codigoBarras, idRemision);
            ErrorMessage = $"Error al vincular pieza: {ex.Message}";
        }

        SelId = idRemision;
        await CargarDatosAsync();
        return Page();
    }

    /// <summary>
    /// Desvincular pieza de la remision.
    /// </summary>
    public async Task<IActionResult> OnPostDesvincularAsync(
        int idRemision, string codigoBarras)
    {
        try
        {
            await _service.DesvincularPiezaAsync(codigoBarras);
            SuccessMessage = $"Pieza {codigoBarras} desvinculada.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error desvinculando pieza {CB}", codigoBarras);
            ErrorMessage = $"Error al desvincular: {ex.Message}";
        }

        SelId = idRemision;
        await CargarDatosAsync();
        return Page();
    }

    /// <summary>
    /// Vincular remision completa (mover piezas de otra remision a esta).
    /// </summary>
    public async Task<IActionResult> OnPostVincularCompletaAsync(
        int idRemision, int idRemisionOrigen)
    {
        try
        {
            await _service.VincularRemisionCompletaAsync(idRemision, idRemisionOrigen);
            SuccessMessage = $"Piezas de remision #{idRemisionOrigen} movidas a #{idRemision}.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moviendo piezas de remision {Orig} a {Dest}",
                idRemisionOrigen, idRemision);
            ErrorMessage = $"Error: {ex.Message}";
        }

        SelId = idRemision;
        await CargarDatosAsync();
        return Page();
    }

    private async Task CargarDatosAsync()
    {
        try
        {
            Proveedores = await _service.ObtenerProveedoresAsync();
            Remisiones = await _service.BuscarRemisionesAsync(Buscar, FiltroProveedor, SoloConsignacion);

            if (SelId.HasValue)
            {
                RemisionActual = await _service.ObtenerRemisionAsync(SelId.Value);
                if (RemisionActual != null)
                {
                    PiezasDisponibles = await _service.ObtenerPiezasDisponiblesAsync(
                        SelId.Value, BuscarPieza);
                    PiezasRemision = await _service.ObtenerPiezasRemisionAsync(SelId.Value);
                    Totales = await _service.ObtenerTotalesRemisionAsync(SelId.Value);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cargando datos de remisiones");
            ErrorMessage = $"Error al consultar datos: {ex.Message}";
        }
    }
}
