using Dapper;
using DiamondsWeb.Models.Reporting;
using DiamondsWeb.Pages.Shared;
using DiamondsWeb.Services.Reporting;
using Microsoft.AspNetCore.Mvc;

namespace DiamondsWeb.Pages.Inventario.InventarioFisico;

/// <summary>
/// Reporte de Inventario Físico.
/// VB6 equivalent: frmReporteInventarioFisico.frm + frmInventarioFisico.frm → ImprimirDB.
/// Muestra conteo de inventario vs existencia real.
/// </summary>
public class ReporteModel : BaseReportPageModel
{
    public ReporteModel(
        ReportDataBuilder reportData,
        ILogger<ReporteModel> logger)
        : base(reportData, logger)
    {
    }

    [BindProperty(SupportsGet = true)] public int? IdPeriodo { get; set; }
    [BindProperty(SupportsGet = true)] public string? Buscar { get; set; }

    protected override string GetTitle() => "Reporte de Inventario Fisico";

    protected override string? GetBackUrl() => "/Inventario/InventarioFisico";

    protected override List<ReportColumn> GetColumns() => new()
    {
        ReportColumn.Text("CodigoBarras", "Codigo", 1.2f),
        ReportColumn.Text("Descripcion", "Descripcion", 2.5f),
        ReportColumn.Text("Grupo", "Grupo", 0.8f),
        ReportColumn.Number("Peso", "Peso", "N2", 0.6f),
        ReportColumn.Currency("Precio", "Precio", "C0", 0.8f),
        ReportColumn.Text("Ubicacion", "Ubicacion", 0.8f),
        ReportColumn.Text("StatusInventario", "Status Inv.", 0.8f),
        ReportColumn.Date("FechaConteo", "Fecha Conteo", "dd/MM/yyyy", 0.8f),
    };

    protected override Task<(string sql, object? parameters)> BuildQueryAsync()
    {
        var where = "WHERE 1=1";
        var p = new DynamicParameters();

        if (IdPeriodo.HasValue)
        {
            where += " AND inv.IdPeriodo = @IdPeriodo";
            p.Add("IdPeriodo", IdPeriodo);
        }
        if (!string.IsNullOrWhiteSpace(Buscar))
        {
            where += " AND (pz.CodigoBarras LIKE @Buscar OR pz.Descripcion LIKE @Buscar)";
            p.Add("Buscar", $"%{Buscar}%");
        }

        var sql = $@"SELECT TOP 5000
            pz.CodigoBarras, pz.Descripcion,
            ISNULL(g.Grupo1, '') AS Grupo,
            pz.Peso, pz.Precio,
            ISNULL(inv.Ubicacion, '') AS Ubicacion,
            CASE WHEN inv.Encontrado = 1 THEN 'Encontrado'
                 WHEN inv.Encontrado = 0 THEN 'Faltante'
                 ELSE 'Pendiente' END AS StatusInventario,
            inv.FechaConteo
        FROM InventarioFisico inv
        INNER JOIN piezas pz ON pz.CodigoBarras = inv.CodigoBarras
        LEFT JOIN grupos g ON g.IdGrupo = pz.IdGrupo
        {where}
        ORDER BY inv.FechaConteo DESC, pz.CodigoBarras";

        return Task.FromResult<(string, object?)>((sql, p));
    }

    protected override string BuildFilterDescription()
    {
        var parts = new List<string>();
        if (IdPeriodo.HasValue) parts.Add($"Periodo: {IdPeriodo}");
        if (!string.IsNullOrWhiteSpace(Buscar)) parts.Add($"Buscar: {Buscar}");
        return string.Join(" | ", parts);
    }

    protected override Task<List<ReportFilterDef>> GetFiltersAsync() =>
        Task.FromResult(new List<ReportFilterDef>
        {
            ReportFilterDef.TextFilter("IdPeriodo", "Periodo", IdPeriodo?.ToString(), "ID del periodo"),
            ReportFilterDef.TextFilter("Buscar", "Buscar", Buscar, "Codigo, descripcion..."),
        });
}
