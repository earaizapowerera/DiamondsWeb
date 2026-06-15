using System.Data;
using ClosedXML.Excel;
using Dapper;
using DiamondsWeb.Models;
using Microsoft.Data.SqlClient;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio de reportes para piezas sencillas.
/// Genera listados con totales, exporta a Excel y PDF.
/// Replica la funcionalidad de ImprimirDB del VB6 legacy.
/// </summary>
public class ReportePiezasService
{
    private readonly string _connectionString;
    private readonly ILogger<ReportePiezasService> _logger;

    public ReportePiezasService(string connectionString, ILogger<ReportePiezasService> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    /// <summary>
    /// Construye la cláusula WHERE dinámica y los parámetros.
    /// </summary>
    private static (string where, DynamicParameters param) BuildFilters(
        string? buscar, int? idGrupo, int? proveedor)
    {
        var where = "WHERE 1=1";
        var p = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            where += " AND (p.CodigoBarras LIKE @Buscar OR p.Descripcion LIKE @Buscar" +
                     " OR p.Modelo LIKE @Buscar OR p.NumSerie LIKE @Buscar)";
            p.Add("Buscar", $"%{buscar}%");
        }
        if (idGrupo.HasValue)
        {
            where += " AND p.IdGrupo = @IdGrupo";
            p.Add("IdGrupo", idGrupo);
        }
        if (proveedor.HasValue)
        {
            where += " AND p.Proveedor = @Proveedor";
            p.Add("Proveedor", proveedor);
        }

        return (where, p);
    }

    /// <summary>
    /// Obtiene piezas con campos extendidos para el reporte.
    /// </summary>
    public async Task<List<PiezaReporte>> ObtenerPiezasParaReporteAsync(
        string? buscar, int? idGrupo, int? proveedor)
    {
        var (where, param) = BuildFilters(buscar, idGrupo, proveedor);
        var sql = $@"SELECT TOP 5000
                p.CodigoBarras, p.Descripcion, g.Grupo1 AS Grupo,
                pr.NombreProveedor, p.Peso,
                p.CBPieza, p.CNPieza, p.CBTotal, p.CNTotal,
                p.Precio, p.Kilates, p.Modelo, p.IdStatus, p.FechaCaptura
            FROM piezas p
            LEFT JOIN vProveedores pr ON pr.Proveedor = p.Proveedor
            LEFT JOIN grupos g ON g.IdGrupo = p.IdGrupo
            {where}
            ORDER BY p.FechaCaptura DESC";

        using var conn = CreateConnection();
        return (await conn.QueryAsync<PiezaReporte>(sql, param)).ToList();
    }

    /// <summary>
    /// Calcula totales del listado con los mismos filtros.
    /// Replica: sum(Peso, CBTotal, CNTotal, CBPieza, CNPieza, Precio) from vpiezas.
    /// </summary>
    public async Task<TotalesPiezas> ObtenerTotalesAsync(
        string? buscar, int? idGrupo, int? proveedor)
    {
        var (where, param) = BuildFilters(buscar, idGrupo, proveedor);
        var sql = $@"SELECT
                ISNULL(SUM(p.Peso), 0) AS Peso,
                ISNULL(SUM(p.CBPieza), 0) AS CBPieza,
                ISNULL(SUM(p.CNPieza), 0) AS CNPieza,
                ISNULL(SUM(p.CBTotal), 0) AS CBTotal,
                ISNULL(SUM(p.CNTotal), 0) AS CNTotal,
                ISNULL(SUM(CAST(p.Precio AS DECIMAL(18,2))), 0) AS Precio,
                COUNT(*) AS TotalPiezas
            FROM piezas p
            LEFT JOIN vProveedores pr ON pr.Proveedor = p.Proveedor
            LEFT JOIN grupos g ON g.IdGrupo = p.IdGrupo
            {where}";

        using var conn = CreateConnection();
        return await conn.QueryFirstAsync<TotalesPiezas>(sql, param);
    }

    /// <summary>
    /// Exporta el listado a Excel con formato profesional.
    /// </summary>
    public async Task<byte[]> ExportarExcelAsync(
        string? buscar, int? idGrupo, int? proveedor)
    {
        var piezas = await ObtenerPiezasParaReporteAsync(buscar, idGrupo, proveedor);
        var totales = await ObtenerTotalesAsync(buscar, idGrupo, proveedor);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Piezas Sencillas");

        // Titulo
        ws.Cell(1, 1).Value = "Listado de Piezas Sencillas — Diamonds";
        ws.Range(1, 1, 1, 10).Merge().Style.Font.SetBold(true).Font.SetFontSize(14);

        // Filtros aplicados
        var filtroTexto = new List<string>();
        if (!string.IsNullOrWhiteSpace(buscar)) filtroTexto.Add($"Buscar: {buscar}");
        if (idGrupo.HasValue) filtroTexto.Add($"Grupo: {idGrupo}");
        if (proveedor.HasValue) filtroTexto.Add($"Proveedor: {proveedor}");
        ws.Cell(2, 1).Value = filtroTexto.Count > 0
            ? $"Filtros: {string.Join(" | ", filtroTexto)}"
            : "Sin filtros (todos)";
        ws.Cell(2, 1).Style.Font.Italic = true;

        // Encabezados
        var headers = new[] { "Codigo", "Descripcion", "Grupo", "Proveedor",
            "Peso", "CB Pieza", "CN Pieza", "CB Total", "CN Total", "Precio",
            "Kilates", "Modelo", "Status", "Fecha" };

        for (int c = 0; c < headers.Length; c++)
            ws.Cell(4, c + 1).Value = headers[c];

        var hdr = ws.Range(4, 1, 4, headers.Length);
        hdr.Style.Font.Bold = true;
        hdr.Style.Fill.BackgroundColor = XLColor.FromHtml("#2d3436");
        hdr.Style.Font.FontColor = XLColor.White;

        // Datos
        for (int i = 0; i < piezas.Count; i++)
        {
            var p = piezas[i];
            var row = i + 5;
            ws.Cell(row, 1).Value = p.CodigoBarras;
            ws.Cell(row, 2).Value = p.Descripcion;
            ws.Cell(row, 3).Value = p.Grupo ?? "";
            ws.Cell(row, 4).Value = p.NombreProveedor ?? "";
            SetDecimalCell(ws, row, 5, p.Peso, "#,##0.00");
            SetDecimalCell(ws, row, 6, p.CBPieza, "$#,##0.00");
            SetDecimalCell(ws, row, 7, p.CNPieza, "$#,##0.00");
            SetDecimalCell(ws, row, 8, p.CBTotal, "$#,##0.00");
            SetDecimalCell(ws, row, 9, p.CNTotal, "$#,##0.00");
            ws.Cell(row, 10).Value = p.Precio ?? 0;
            ws.Cell(row, 10).Style.NumberFormat.Format = "$#,##0";
            ws.Cell(row, 11).Value = p.Kilates ?? "";
            ws.Cell(row, 12).Value = p.Modelo ?? "";
            ws.Cell(row, 13).Value = p.StatusNombre;
            if (p.FechaCaptura.HasValue)
            {
                ws.Cell(row, 14).Value = p.FechaCaptura.Value;
                ws.Cell(row, 14).Style.NumberFormat.Format = "dd/MM/yyyy";
            }
        }

        // Fila de totales
        var totalRow = piezas.Count + 5;
        ws.Cell(totalRow, 1).Value = $"TOTALES ({totales.TotalPiezas} piezas)";
        ws.Cell(totalRow, 1).Style.Font.Bold = true;
        SetDecimalCell(ws, totalRow, 5, totales.Peso, "#,##0.00", true);
        SetDecimalCell(ws, totalRow, 6, totales.CBPieza, "$#,##0.00", true);
        SetDecimalCell(ws, totalRow, 7, totales.CNPieza, "$#,##0.00", true);
        SetDecimalCell(ws, totalRow, 8, totales.CBTotal, "$#,##0.00", true);
        SetDecimalCell(ws, totalRow, 9, totales.CNTotal, "$#,##0.00", true);
        ws.Cell(totalRow, 10).Value = totales.Precio;
        ws.Cell(totalRow, 10).Style.NumberFormat.Format = "$#,##0";
        ws.Cell(totalRow, 10).Style.Font.Bold = true;

        var totalRange = ws.Range(totalRow, 1, totalRow, headers.Length);
        totalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#dfe6e9");
        totalRange.Style.Border.TopBorder = XLBorderStyleValues.Double;

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static void SetDecimalCell(IXLWorksheet ws, int row, int col,
        decimal? value, string format, bool bold = false)
    {
        ws.Cell(row, col).Value = value ?? 0m;
        ws.Cell(row, col).Style.NumberFormat.Format = format;
        if (bold) ws.Cell(row, col).Style.Font.Bold = true;
    }

    /// <summary>
    /// Exporta el listado a PDF con QuestPDF.
    /// </summary>
    public async Task<byte[]> ExportarPdfAsync(
        string? buscar, int? idGrupo, int? proveedor)
    {
        var piezas = await ObtenerPiezasParaReporteAsync(buscar, idGrupo, proveedor);
        var totales = await ObtenerTotalesAsync(buscar, idGrupo, proveedor);

        var filtroTexto = BuildFilterDescription(buscar, idGrupo, proveedor);

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Column(col =>
                {
                    col.Item().Text("Diamonds — Listado de Piezas Sencillas")
                        .FontSize(14).Bold();
                    col.Item().Text(filtroTexto).FontSize(8).Italic();
                    col.Item().Text($"Generado: {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC")
                        .FontSize(7);
                    col.Item().PaddingBottom(8);
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(1.2f); // Codigo
                        cols.RelativeColumn(2.5f); // Descripcion
                        cols.RelativeColumn(0.8f); // Grupo
                        cols.RelativeColumn(1.2f); // Proveedor
                        cols.RelativeColumn(0.7f); // Peso
                        cols.RelativeColumn(0.8f); // CBPieza
                        cols.RelativeColumn(0.8f); // CNPieza
                        cols.RelativeColumn(0.8f); // CBTotal
                        cols.RelativeColumn(0.8f); // CNTotal
                        cols.RelativeColumn(0.8f); // Precio
                        cols.RelativeColumn(0.5f); // Kilates
                        cols.RelativeColumn(0.7f); // Status
                        cols.RelativeColumn(0.8f); // Fecha
                    });

                    // Encabezados
                    var headerLabels = new[] { "Codigo", "Descripcion", "Grupo",
                        "Proveedor", "Peso", "CB Pieza", "CN Pieza", "CB Total",
                        "CN Total", "Precio", "Kilates", "Status", "Fecha" };

                    table.Header(header =>
                    {
                        foreach (var label in headerLabels)
                        {
                            header.Cell().Background(Colors.Grey.Darken3)
                                .Padding(3).Text(label)
                                .FontColor(Colors.White).Bold().FontSize(7);
                        }
                    });

                    // Filas
                    foreach (var p in piezas)
                    {
                        var bg = piezas.IndexOf(p) % 2 == 0
                            ? Colors.White : Colors.Grey.Lighten4;

                        PdfCell(table, p.CodigoBarras, bg);
                        PdfCell(table, p.Descripcion, bg);
                        PdfCell(table, p.Grupo ?? "", bg);
                        PdfCell(table, p.NombreProveedor ?? "", bg);
                        PdfCellRight(table, p.Peso?.ToString("N2") ?? "", bg);
                        PdfCellRight(table, p.CBPieza?.ToString("C2") ?? "", bg);
                        PdfCellRight(table, p.CNPieza?.ToString("C2") ?? "", bg);
                        PdfCellRight(table, p.CBTotal?.ToString("C2") ?? "", bg);
                        PdfCellRight(table, p.CNTotal?.ToString("C2") ?? "", bg);
                        PdfCellRight(table, p.Precio?.ToString("C0") ?? "", bg);
                        PdfCell(table, p.Kilates ?? "", bg);
                        PdfCell(table, p.StatusNombre, bg);
                        PdfCell(table, p.FechaCaptura?.ToString("dd/MM/yyyy") ?? "", bg);
                    }

                    // Fila de totales
                    var tBg = Colors.Grey.Lighten2;
                    PdfCellBold(table, $"TOTALES ({totales.TotalPiezas})", tBg);
                    PdfCell(table, "", tBg);
                    PdfCell(table, "", tBg);
                    PdfCell(table, "", tBg);
                    PdfCellRightBold(table, totales.Peso.ToString("N2"), tBg);
                    PdfCellRightBold(table, totales.CBPieza.ToString("C2"), tBg);
                    PdfCellRightBold(table, totales.CNPieza.ToString("C2"), tBg);
                    PdfCellRightBold(table, totales.CBTotal.ToString("C2"), tBg);
                    PdfCellRightBold(table, totales.CNTotal.ToString("C2"), tBg);
                    PdfCellRightBold(table, totales.Precio.ToString("C0"), tBg);
                    PdfCell(table, "", tBg);
                    PdfCell(table, "", tBg);
                    PdfCell(table, "", tBg);
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

    private static string BuildFilterDescription(string? buscar, int? idGrupo, int? proveedor)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(buscar)) parts.Add($"Buscar: {buscar}");
        if (idGrupo.HasValue) parts.Add($"Grupo: {idGrupo}");
        if (proveedor.HasValue) parts.Add($"Proveedor: {proveedor}");
        return parts.Count > 0 ? $"Filtros: {string.Join(" | ", parts)}" : "Sin filtros (todos)";
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
