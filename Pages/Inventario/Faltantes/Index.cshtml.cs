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
            ws.Cell(1, 1).Value = "Codigo Barras";
            ws.Cell(1, 2).Value = "Descripcion";
            ws.Cell(1, 3).Value = "Precio";
            ws.Cell(1, 4).Value = "Grupo";
            ws.Cell(1, 5).Value = "Comentario";

            var headerRange = ws.Range(1, 1, 1, 5);
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
                ws.Cell(row, 3).Value = f.Precio ?? 0;
                ws.Cell(row, 4).Value = f.Grupo ?? "";
                ws.Cell(row, 5).Value = f.Comentario ?? "";
            }

            // Formato de columna precio
            ws.Column(3).Style.NumberFormat.Format = "$#,##0.00";

            // Resumen al final
            int totalRow = datos.Count + 3;
            ws.Cell(totalRow, 2).Value = "Total Faltantes:";
            ws.Cell(totalRow, 2).Style.Font.Bold = true;
            ws.Cell(totalRow, 3).FormulaA1 = $"COUNTA(A2:A{datos.Count + 1})";
            ws.Cell(totalRow, 3).Style.Font.Bold = true;

            int sumaRow = totalRow + 1;
            ws.Cell(sumaRow, 2).Value = "Suma Precios:";
            ws.Cell(sumaRow, 2).Style.Font.Bold = true;
            ws.Cell(sumaRow, 3).FormulaA1 = $"SUM(C2:C{datos.Count + 1})";
            ws.Cell(sumaRow, 3).Style.Font.Bold = true;
            ws.Cell(sumaRow, 3).Style.NumberFormat.Format = "$#,##0.00";

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
