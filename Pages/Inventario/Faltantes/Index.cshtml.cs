using ClosedXML.Excel;
using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Inventario.Faltantes;

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

    public List<PiezaFaltante> Faltantes { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    [BindProperty]
    public string? ComentarioCB { get; set; }

    [BindProperty]
    public string? ComentarioTexto { get; set; }

    public int TotalFaltantes => Faltantes.Count;
    public decimal SumaPrecios => Faltantes.Sum(f => f.Precio ?? 0);
    public int ConComentario => Faltantes.Count(f => !string.IsNullOrWhiteSpace(f.Comentario));
    public int SinComentario => Faltantes.Count(f => string.IsNullOrWhiteSpace(f.Comentario));

    public async Task OnGetAsync()
    {
        try
        {
            Faltantes = await _inventoryService.ObtenerFaltantesAsync(Buscar);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar faltantes");
            TempData["Error"] = $"Error al cargar faltantes: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostComentarioAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ComentarioCB))
            {
                TempData["Error"] = "Codigo de barras requerido.";
                return RedirectToPage(new { Buscar });
            }

            await _inventoryService.GuardarComentarioFaltanteAsync(
                ComentarioCB.Trim(), ComentarioTexto?.Trim() ?? "");
            TempData["Success"] = "Comentario guardado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar comentario para {CB}", ComentarioCB);
            TempData["Error"] = $"Error al guardar comentario: {ex.Message}";
        }

        return RedirectToPage(new { Buscar });
    }

    public async Task<IActionResult> OnPostExportarExcelAsync()
    {
        try
        {
            var datos = await _inventoryService.ObtenerFaltantesAsync(Buscar);

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Faltantes");

            // Encabezados
            string[] headers = { "Codigo Barras", "Descripcion", "Modelo", "Linea",
                                 "Kilates", "Peso", "Precio", "Grupo", "Num Serie", "Comentario" };
            for (int c = 0; c < headers.Length; c++)
                ws.Cell(1, c + 1).Value = headers[c];

            var headerRange = ws.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#343a40");
            headerRange.Style.Font.FontColor = XLColor.White;

            // Datos
            for (int i = 0; i < datos.Count; i++)
            {
                var f = datos[i];
                int row = i + 2;
                ws.Cell(row, 1).Value = f.CodigoBarras;
                ws.Cell(row, 2).Value = f.Descripcion ?? "";
                ws.Cell(row, 3).Value = f.Modelo ?? "";
                ws.Cell(row, 4).Value = f.Linea ?? "";
                ws.Cell(row, 5).Value = f.Kilates ?? "";
                ws.Cell(row, 6).Value = f.Peso ?? 0;
                ws.Cell(row, 7).Value = f.Precio ?? 0;
                ws.Cell(row, 8).Value = f.Grupo ?? "";
                ws.Cell(row, 9).Value = f.NumSerie ?? "";
                ws.Cell(row, 10).Value = f.Comentario ?? "";
            }

            // Formato columnas numericas
            ws.Column(6).Style.NumberFormat.Format = "#,##0.00";
            ws.Column(7).Style.NumberFormat.Format = "$#,##0.00";

            // Resumen al final
            int totalRow = datos.Count + 3;
            ws.Cell(totalRow, 1).Value = "Total Faltantes:";
            ws.Cell(totalRow, 1).Style.Font.Bold = true;
            ws.Cell(totalRow, 2).Value = datos.Count;
            ws.Cell(totalRow, 2).Style.Font.Bold = true;

            int sumaRow = totalRow + 1;
            ws.Cell(sumaRow, 1).Value = "Suma Precios:";
            ws.Cell(sumaRow, 1).Style.Font.Bold = true;
            ws.Cell(sumaRow, 7).FormulaA1 = $"SUM(G2:G{datos.Count + 1})";
            ws.Cell(sumaRow, 7).Style.Font.Bold = true;
            ws.Cell(sumaRow, 7).Style.NumberFormat.Format = "$#,##0.00";

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"Faltantes_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al exportar faltantes a Excel");
            TempData["Error"] = $"Error al exportar: {ex.Message}";
            return RedirectToPage(new { Buscar });
        }
    }
}
