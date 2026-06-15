using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;

namespace DiamondsWeb.Pages.Inventario;

[Authorize]
public class RegistroExistenciasModel : PageModel
{
    private readonly InventarioFisicoService _service;
    private readonly ILogger<RegistroExistenciasModel> _logger;

    public RegistroExistenciasModel(
        InventarioFisicoService service,
        ILogger<RegistroExistenciasModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    public List<InventarioFisicoItem> Registros { get; set; } = new();
    public InventarioStats Stats { get; set; } = new();
    public string? MensajeExito { get; set; }
    public string? MensajeError { get; set; }
    public RegistroResultado? UltimoRegistro { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Filtro { get; set; } = "hoy";

    [BindProperty(SupportsGet = true)]
    public string? Busqueda { get; set; }

    public async Task OnGetAsync()
    {
        await CargarDatosAsync();
    }

    private int ObtenerIdUsuario()
    {
        return int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid
                : throw new UnauthorizedAccessException("IdUsuario claim not found");
    }

    /// <summary>
    /// Registrar existencia por código de barras (POST principal).
    /// </summary>
    public async Task<IActionResult> OnPostRegistrarAsync(string codigoBarras)
    {
        var idUsuario = ObtenerIdUsuario();

        var resultado = await _service.RegistrarExistenciaAsync(codigoBarras, idUsuario);
        UltimoRegistro = resultado;

        if (resultado.Exito)
            MensajeExito = resultado.Mensaje;
        else
            MensajeError = resultado.Mensaje;

        await CargarDatosAsync();
        return Page();
    }

    /// <summary>
    /// Completar datos de sobrante (descripción y precio).
    /// </summary>
    public async Task<IActionResult> OnPostCompletarSobranteAsync(
        string codigoBarras, string? descripcion, decimal? precio)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
        {
            MensajeError = "La descripción es obligatoria para piezas sobrantes.";
            await CargarDatosAsync();
            return Page();
        }

        await _service.ActualizarSobranteAsync(codigoBarras, descripcion, precio);
        MensajeExito = $"Sobrante {codigoBarras} actualizado: {descripcion}";

        await CargarDatosAsync();
        return Page();
    }

    /// <summary>
    /// Cancelar un registro de inventario.
    /// </summary>
    public async Task<IActionResult> OnPostCancelarAsync(int registroId)
    {
        var canceladoPor = ObtenerIdUsuario();
        var resultado = await _service.CancelarRegistroAsync(registroId, canceladoPor);

        if (resultado.Exito)
            MensajeExito = resultado.Mensaje;
        else
            MensajeError = resultado.Mensaje;

        await CargarDatosAsync();
        return Page();
    }

    /// <summary>
    /// Exportar a Excel (CSV con encoding UTF-8 BOM para compatibilidad).
    /// </summary>
    public async Task<IActionResult> OnPostExportarExcelAsync()
    {
        var registros = await _service.ObtenerRegistrosParaExportarAsync(Filtro);

        var sb = new StringBuilder();
        sb.AppendLine("Id,CodigoBarras,Descripcion,Precio,Origen,FechaCaptura,IdUsuario");

        foreach (var r in registros)
        {
            var desc = (r.Descripcion ?? "").Replace("\"", "\"\"");
            sb.AppendLine(
                $"{r.Id},\"{r.CodigoBarras}\",\"{desc}\",{r.Precio?.ToString("F2") ?? ""},\"{r.Origen}\",\"{r.FechaCaptura:yyyy-MM-dd HH:mm:ss}\",{r.IdUsuario}");
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        var fileName = $"RegistroExistencias_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    private async Task CargarDatosAsync()
    {
        try
        {
            Registros = await _service.ObtenerRegistrosAsync(Filtro, Busqueda);
            Stats = await _service.ObtenerEstadisticasAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cargando datos de inventario");
            MensajeError = $"Error al cargar datos: {ex.Message}";
        }
    }
}
