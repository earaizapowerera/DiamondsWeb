using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Inventario;

[Authorize]
public class FaltantesModel : PageModel
{
    private readonly FaltantesService _faltantesService;
    private readonly ILogger<FaltantesModel> _logger;

    public FaltantesModel(FaltantesService faltantesService, ILogger<FaltantesModel> logger)
    {
        _faltantesService = faltantesService;
        _logger = logger;
    }

    public List<PiezaFaltante> Faltantes { get; set; } = new();
    public FaltantesStats Stats { get; set; } = new();
    public List<string> StatusDisponibles { get; set; } = new();
    public string? ErrorMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FiltroStatus { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Faltantes = await _faltantesService.ObtenerFaltantesAsync(Buscar, FiltroStatus);
            Stats = await _faltantesService.ObtenerEstadisticasAsync();
            StatusDisponibles = await _faltantesService.ObtenerStatusDisponiblesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cargando faltantes");
            ErrorMessage = $"Error al consultar datos: {ex.Message}";
        }
    }

    /// <summary>
    /// Guarda comentario para una pieza faltante (AJAX POST).
    /// </summary>
    public async Task<IActionResult> OnPostGuardarComentarioAsync(
        string codigoBarras, string? comentarios)
    {
        try
        {
            await _faltantesService.GuardarComentarioAsync(codigoBarras, comentarios);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error guardando comentario para {CodigoBarras}", codigoBarras);
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Exporta faltantes a Excel (ClosedXML).
    /// Replica boton XLS del toolbar VB6.
    /// </summary>
    public async Task<IActionResult> OnGetExportarExcelAsync()
    {
        var faltantes = await _faltantesService.ObtenerFaltantesAsync(Buscar, FiltroStatus);

        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var ws = workbook.Worksheets.Add("Faltantes");

        // Encabezados
        var headers = new[] { "Codigo", "Descripcion", "Modelo", "Linea", "Kilates",
                              "Peso", "Costo Total", "Precio", "Status", "Comentarios" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
        }

        // Datos
        for (int r = 0; r < faltantes.Count; r++)
        {
            var f = faltantes[r];
            ws.Cell(r + 2, 1).Value = f.CodigoBarras;
            ws.Cell(r + 2, 2).Value = f.Descripcion ?? "";
            ws.Cell(r + 2, 3).Value = f.Modelo ?? "";
            ws.Cell(r + 2, 4).Value = f.Linea ?? "";
            ws.Cell(r + 2, 5).Value = f.Kilates ?? "";
            ws.Cell(r + 2, 6).Value = f.Peso;
            ws.Cell(r + 2, 7).Value = f.CBTotal;
            ws.Cell(r + 2, 8).Value = f.Precio;
            ws.Cell(r + 2, 9).Value = f.Status ?? "";
            ws.Cell(r + 2, 10).Value = f.Comentarios ?? "";
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var fileName = $"Faltantes_{DateTime.UtcNow:yyyyMMdd}.xlsx";
        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}
