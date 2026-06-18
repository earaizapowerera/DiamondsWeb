using Dapper;
using DiamondsWeb.Models.Reporting;
using DiamondsWeb.Pages.Shared;
using DiamondsWeb.Services.Reporting;
using Microsoft.AspNetCore.Mvc;

namespace DiamondsWeb.Pages.Ventas.ConsultaBajas;

/// <summary>
/// Reporte de Piezas Vendidas (Bajas).
/// VB6 equivalent: frmConsultaBajas.frm → ImprimirDB
/// con totales: sum(Precio) from bajaspiezas + AutoBusquedaWhere.
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
    [BindProperty(SupportsGet = true)] public string? Buscar { get; set; }

    protected override string GetTitle() => "Reporte de Piezas Vendidas (Bajas)";

    protected override string? GetBackUrl() => "/Ventas/ConsultaBajas";

    protected override List<ReportColumn> GetColumns() => new()
    {
        ReportColumn.Text("CodigoBarras", "Codigo", 1.2f),
        ReportColumn.Text("Descripcion", "Descripcion", 2f),
        ReportColumn.Text("Grupo", "Grupo", 0.8f),
        ReportColumn.Text("NombreProveedor", "Proveedor", 1f),
        ReportColumn.Number("Peso", "Peso", "N2", 0.6f),
        ReportColumn.Currency("Precio", "Precio", "C0", 0.8f),
        ReportColumn.Text("IdNota", "Nota", 0.5f),
        ReportColumn.Date("FechaBaja", "Fecha Baja", "dd/MM/yyyy", 0.8f),
        ReportColumn.Text("NombreCliente", "Cliente", 1.5f),
    };

    protected override Task<(string sql, object? parameters)> BuildQueryAsync()
    {
        var where = "WHERE 1=1";
        var p = new DynamicParameters();

        if (Desde.HasValue)
        {
            where += " AND bp.FechaBaja >= @Desde";
            p.Add("Desde", Desde.Value.Date);
        }
        if (Hasta.HasValue)
        {
            where += " AND bp.FechaBaja <= @Hasta";
            p.Add("Hasta", Hasta.Value.Date.AddDays(1));
        }
        if (!string.IsNullOrWhiteSpace(Buscar))
        {
            where += " AND (bp.CodigoBarras LIKE @Buscar OR bp.Descripcion LIKE @Buscar)";
            p.Add("Buscar", $"%{Buscar}%");
        }

        // VB6: bajaspiezas con joins
        var sql = $@"SELECT TOP 5000
            bp.CodigoBarras, bp.Descripcion,
            ISNULL(g.Grupo1, '') AS Grupo,
            ISNULL(pr.NombreProveedor, '') AS NombreProveedor,
            bp.Peso, bp.Precio, bp.IdNota,
            bp.FechaBaja,
            ISNULL(bn.NombreCliente, '') AS NombreCliente
        FROM bajaspiezas bp
        LEFT JOIN grupos g ON g.IdGrupo = bp.IdGrupo
        LEFT JOIN vProveedores pr ON pr.Proveedor = bp.Proveedor
        LEFT JOIN bajasnotas bn ON bn.IdNota = bp.IdNota
        {where}
        ORDER BY bp.FechaBaja DESC";

        return Task.FromResult<(string, object?)>((sql, p));
    }

    protected override string BuildFilterDescription()
    {
        var parts = new List<string>();
        if (Desde.HasValue) parts.Add($"Desde: {Desde:dd/MM/yyyy}");
        if (Hasta.HasValue) parts.Add($"Hasta: {Hasta:dd/MM/yyyy}");
        if (!string.IsNullOrWhiteSpace(Buscar)) parts.Add($"Buscar: {Buscar}");
        return string.Join(" | ", parts);
    }

    protected override Task<List<ReportFilterDef>> GetFiltersAsync() =>
        Task.FromResult(new List<ReportFilterDef>
        {
            ReportFilterDef.DateFilter("Desde", "Desde", Desde?.ToString("yyyy-MM-dd")),
            ReportFilterDef.DateFilter("Hasta", "Hasta", Hasta?.ToString("yyyy-MM-dd")),
            ReportFilterDef.TextFilter("Buscar", "Buscar", Buscar, "Codigo, descripcion..."),
        });
}
