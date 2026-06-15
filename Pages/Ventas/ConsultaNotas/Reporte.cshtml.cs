using Dapper;
using DiamondsWeb.Models.Reporting;
using DiamondsWeb.Pages.Shared;
using DiamondsWeb.Services.Reporting;
using Microsoft.AspNetCore.Mvc;

namespace DiamondsWeb.Pages.Ventas.ConsultaNotas;

/// <summary>
/// Reporte de Notas de Venta.
/// VB6 equivalent: frmConsultaNotas.frm → ImprimirDB
/// con totales: sum(Neto) from vbajasnotas.
/// También usaba Crystal Reports para NotasComprimidas.rpt.
/// </summary>
public class ReporteModel : BaseReportPageModel
{
    public ReporteModel(
        ReportDataBuilder reportData,
        ILogger<ReporteModel> logger)
        : base(reportData, logger)
    {
    }

    [BindProperty(SupportsGet = true)] public DateTime? Desde { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? Hasta { get; set; }
    [BindProperty(SupportsGet = true)] public string? NombreCliente { get; set; }

    protected override string GetTitle() => "Reporte de Notas de Venta";

    protected override string? GetBackUrl() => "/Ventas/ConsultaNotas";

    protected override List<ReportColumn> GetColumns() => new()
    {
        ReportColumn.Text("IdNota", "Nota", 0.6f),
        ReportColumn.Date("Fecha", "Fecha", "dd/MM/yyyy", 0.8f),
        ReportColumn.Text("NombreCliente", "Cliente", 2f),
        ReportColumn.Number("Piezas", "Piezas", "N0", 0.5f, summable: true),
        ReportColumn.Currency("Bruto", "Bruto", "C2", 1f),
        ReportColumn.Currency("Descuento", "Descuento", "C2", 0.8f),
        ReportColumn.Currency("Neto", "Neto", "C2", 1f),
        ReportColumn.Text("FormaPago", "Forma Pago", 0.8f),
        ReportColumn.Text("Vendedor", "Vendedor", 1f),
    };

    protected override Task<(string sql, object? parameters)> BuildQueryAsync()
    {
        var where = "WHERE 1=1";
        var p = new DynamicParameters();

        if (Desde.HasValue)
        {
            where += " AND bn.Fecha >= @Desde";
            p.Add("Desde", Desde.Value.Date);
        }
        if (Hasta.HasValue)
        {
            where += " AND bn.Fecha <= @Hasta";
            p.Add("Hasta", Hasta.Value.Date.AddDays(1));
        }
        if (!string.IsNullOrWhiteSpace(NombreCliente))
        {
            where += " AND bn.NombreCliente LIKE @NombreCliente";
            p.Add("NombreCliente", $"%{NombreCliente}%");
        }

        var sql = $@"SELECT TOP 5000
            bn.IdNota, bn.Fecha, bn.NombreCliente,
            bn.Piezas, bn.Bruto, bn.Descuento, bn.Neto,
            ISNULL(bn.FormaPago, '') AS FormaPago,
            ISNULL(bn.Vendedor, '') AS Vendedor
        FROM vbajasnotas bn
        {where}
        ORDER BY bn.Fecha DESC, bn.IdNota DESC";

        return Task.FromResult<(string, object?)>((sql, p));
    }

    protected override string BuildFilterDescription()
    {
        var parts = new List<string>();
        if (Desde.HasValue) parts.Add($"Desde: {Desde:dd/MM/yyyy}");
        if (Hasta.HasValue) parts.Add($"Hasta: {Hasta:dd/MM/yyyy}");
        if (!string.IsNullOrWhiteSpace(NombreCliente)) parts.Add($"Cliente: {NombreCliente}");
        return string.Join(" | ", parts);
    }

    protected override Task<List<ReportFilterDef>> GetFiltersAsync() =>
        Task.FromResult(new List<ReportFilterDef>
        {
            ReportFilterDef.DateFilter("Desde", "Desde", Desde?.ToString("yyyy-MM-dd")),
            ReportFilterDef.DateFilter("Hasta", "Hasta", Hasta?.ToString("yyyy-MM-dd")),
            ReportFilterDef.TextFilter("NombreCliente", "Cliente", NombreCliente, "Nombre del cliente..."),
        });
}
