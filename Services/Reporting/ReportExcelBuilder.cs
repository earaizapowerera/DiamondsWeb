using ClosedXML.Excel;
using DiamondsWeb.Models.Reporting;

namespace DiamondsWeb.Services.Reporting;

/// <summary>
/// Genera archivos Excel a partir de un ReportResult.
/// Reemplaza la funcionalidad ImprimirDB del VB6 para exportación a hoja de cálculo.
/// Usa ClosedXML (ya incluido en el proyecto).
/// </summary>
public static class ReportExcelBuilder
{
    public static byte[] Build(ReportResult report)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add(TruncateSheetName(report.Definition.Title));

        // Título
        ws.Cell(1, 1).Value = $"{report.Definition.Title} — Diamonds";
        ws.Range(1, 1, 1, report.Definition.Columns.Count)
            .Merge().Style.Font.SetBold(true).Font.SetFontSize(14);

        // Subtítulo/filtros
        var filterText = !string.IsNullOrWhiteSpace(report.Definition.FilterDescription)
            ? $"Filtros: {report.Definition.FilterDescription}"
            : "Sin filtros (todos)";
        ws.Cell(2, 1).Value = filterText;
        ws.Cell(2, 1).Style.Font.Italic = true;

        ws.Cell(3, 1).Value = $"Generado: {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC";
        ws.Cell(3, 1).Style.Font.FontSize = 9;
        ws.Cell(3, 1).Style.Font.FontColor = XLColor.Gray;

        var cols = report.Definition.Columns;

        // Encabezados (fila 5)
        for (int c = 0; c < cols.Count; c++)
        {
            ws.Cell(5, c + 1).Value = cols[c].Header;
        }
        var hdr = ws.Range(5, 1, 5, cols.Count);
        hdr.Style.Font.Bold = true;
        hdr.Style.Fill.BackgroundColor = XLColor.FromHtml("#2d3436");
        hdr.Style.Font.FontColor = XLColor.White;

        // Datos
        for (int r = 0; r < report.Rows.Count; r++)
        {
            var row = report.Rows[r];
            var xlRow = r + 6;

            for (int c = 0; c < cols.Count; c++)
            {
                var col = cols[c];
                var val = row.GetValueOrDefault(col.Field);
                SetCellValue(ws.Cell(xlRow, c + 1), val, col);
            }
        }

        // Fila de totales
        if (report.Totals.Count > 0)
        {
            var totalRow = report.Rows.Count + 6;
            ws.Cell(totalRow, 1).Value = $"TOTALES ({report.TotalRows} registros)";
            ws.Cell(totalRow, 1).Style.Font.Bold = true;

            for (int c = 0; c < cols.Count; c++)
            {
                var col = cols[c];
                if (report.Totals.TryGetValue(col.Field, out var total))
                {
                    var cell = ws.Cell(totalRow, c + 1);
                    cell.Value = (double)total;
                    cell.Style.Font.Bold = true;
                    if (!string.IsNullOrEmpty(col.ExcelFormat))
                        cell.Style.NumberFormat.Format = col.ExcelFormat;
                }
            }

            var totalRange = ws.Range(totalRow, 1, totalRow, cols.Count);
            totalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#dfe6e9");
            totalRange.Style.Border.TopBorder = XLBorderStyleValues.Double;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static void SetCellValue(IXLCell cell, object? value, ReportColumn col)
    {
        if (value == null || value == DBNull.Value)
        {
            cell.Value = "";
            return;
        }

        if (value is DateTime dt)
        {
            cell.Value = dt;
            cell.Style.NumberFormat.Format = !string.IsNullOrEmpty(col.ExcelFormat)
                ? col.ExcelFormat : "dd/MM/yyyy";
            return;
        }

        if (value is decimal d)
        {
            cell.Value = (double)d;
            if (!string.IsNullOrEmpty(col.ExcelFormat))
                cell.Style.NumberFormat.Format = col.ExcelFormat;
            return;
        }

        if (value is int i)
        {
            cell.Value = i;
            if (!string.IsNullOrEmpty(col.ExcelFormat))
                cell.Style.NumberFormat.Format = col.ExcelFormat;
            return;
        }

        if (value is double dbl)
        {
            cell.Value = dbl;
            if (!string.IsNullOrEmpty(col.ExcelFormat))
                cell.Style.NumberFormat.Format = col.ExcelFormat;
            return;
        }

        cell.Value = value.ToString() ?? "";
    }

    private static string TruncateSheetName(string name) =>
        name.Length > 31 ? name[..31] : name;
}
