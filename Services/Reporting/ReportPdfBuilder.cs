using DiamondsWeb.Models.Reporting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DiamondsWeb.Services.Reporting;

/// <summary>
/// Genera PDF a partir de un ReportResult usando QuestPDF.
/// Reemplaza Crystal Reports del VB6.
/// Genera reportes tabulares con encabezado, filtros, datos y totales.
/// </summary>
public static class ReportPdfBuilder
{
    public static byte[] Build(ReportResult report)
    {
        var def = report.Definition;

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                if (def.LandscapeOrientation)
                    page.Size(PageSizes.Letter.Landscape());
                else
                    page.Size(PageSizes.Letter);

                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Column(col =>
                {
                    col.Item().Text($"Diamonds — {def.Title}")
                        .FontSize(14).Bold();

                    if (!string.IsNullOrWhiteSpace(def.Subtitle))
                        col.Item().Text(def.Subtitle).FontSize(9);

                    if (!string.IsNullOrWhiteSpace(def.FilterDescription))
                        col.Item().Text($"Filtros: {def.FilterDescription}")
                            .FontSize(8).Italic();

                    col.Item().Text($"Generado: {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC")
                        .FontSize(7);
                    col.Item().PaddingBottom(8);
                });

                page.Content().Table(table =>
                {
                    // Definir columnas
                    table.ColumnsDefinition(cols =>
                    {
                        foreach (var c in def.Columns)
                            cols.RelativeColumn(c.RelativeWidth);
                    });

                    // Encabezados
                    table.Header(header =>
                    {
                        foreach (var c in def.Columns)
                        {
                            var cell = header.Cell().Background(Colors.Grey.Darken3)
                                .Padding(3);

                            if (c.Align == ColumnAlign.Right)
                                cell.AlignRight().Text(c.Header)
                                    .FontColor(Colors.White).Bold().FontSize(7);
                            else
                                cell.Text(c.Header)
                                    .FontColor(Colors.White).Bold().FontSize(7);
                        }
                    });

                    // Filas de datos
                    for (int r = 0; r < report.Rows.Count; r++)
                    {
                        var row = report.Rows[r];
                        var bg = r % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                        foreach (var c in def.Columns)
                        {
                            var val = row.GetValueOrDefault(c.Field);
                            var text = FormatValue(val, c.Format);

                            if (c.Align == ColumnAlign.Right)
                                PdfCellRight(table, text, bg);
                            else
                                PdfCell(table, text, bg);
                        }
                    }

                    // Fila de totales
                    if (report.Totals.Count > 0)
                    {
                        var tBg = Colors.Grey.Lighten2;
                        bool first = true;

                        foreach (var c in def.Columns)
                        {
                            if (first)
                            {
                                PdfCellBold(table, $"TOTALES ({report.TotalRows})", tBg);
                                first = false;
                                continue;
                            }

                            if (report.Totals.TryGetValue(c.Field, out var total))
                            {
                                var text = FormatValue(total, c.Format);
                                PdfCellRightBold(table, text, tBg);
                            }
                            else
                            {
                                PdfCell(table, "", tBg);
                            }
                        }
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Pagina ");
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                });
            });
        });

        return doc.GeneratePdf();
    }

    private static string FormatValue(object? value, string format)
    {
        if (value == null || value == DBNull.Value) return "";
        if (string.IsNullOrEmpty(format)) return value.ToString() ?? "";

        return value switch
        {
            decimal d => d.ToString(format),
            double dbl => dbl.ToString(format),
            int i => i.ToString(format),
            DateTime dt => dt.ToString(format),
            _ => value.ToString() ?? ""
        };
    }

    private static void PdfCell(TableDescriptor t, string text, Color bg) =>
        t.Cell().Background(bg).Padding(3).Text(text).FontSize(7);

    private static void PdfCellRight(TableDescriptor t, string text, Color bg) =>
        t.Cell().Background(bg).Padding(3).AlignRight().Text(text).FontSize(7);

    private static void PdfCellBold(TableDescriptor t, string text, Color bg) =>
        t.Cell().Background(bg).Padding(3).Text(text).Bold().FontSize(7);

    private static void PdfCellRightBold(TableDescriptor t, string text, Color bg) =>
        t.Cell().Background(bg).Padding(3).AlignRight().Text(text).Bold().FontSize(7);
}
