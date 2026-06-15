using Dapper;
using DiamondsWeb.Models.Reporting;
using DiamondsWeb.Pages.Shared;
using DiamondsWeb.Services;
using DiamondsWeb.Services.Reporting;
using Microsoft.AspNetCore.Mvc;

namespace DiamondsWeb.Pages.Inventario.Existencias;

/// <summary>
/// Reporte de Existencias (Registro de Existencias).
/// VB6 equivalent: frmRegistroExistencias.frm → ImprimirDB.
/// Muestra el resumen de existencias por grupo con totales.
/// </summary>
public class ReporteModel : BaseReportPageModel
{
    private readonly CatalogService _catalogService;

    public ReporteModel(
        ReportDataBuilder reportData,
        CatalogService catalogService,
        ILogger<ReporteModel> logger)
        : base(reportData, logger)
    {
        _catalogService = catalogService;
    }

    [BindProperty(SupportsGet = true)] public int? IdGrupo { get; set; }

    protected override string GetTitle() => "Registro de Existencias";

    protected override string? GetBackUrl() => "/Inventario/Existencias";

    protected override bool IsLandscape() => true;

    protected override List<ReportColumn> GetColumns() => new()
    {
        ReportColumn.Text("Grupo", "Grupo", 1.5f),
        ReportColumn.Number("CantidadPiezas", "Piezas", "N0", 0.7f),
        ReportColumn.Number("TotalPeso", "Peso Total", "N2", 0.8f),
        ReportColumn.Currency("TotalCBTotal", "CB Total", "C2", 1f),
        ReportColumn.Currency("TotalCNTotal", "CN Total", "C2", 1f),
        ReportColumn.Currency("TotalPrecio", "Precio Total", "C0", 1f),
    };

    protected override Task<(string sql, object? parameters)> BuildQueryAsync()
    {
        var where = "WHERE p.IdStatus = 1";
        var p = new DynamicParameters();

        if (IdGrupo.HasValue)
        {
            where += " AND p.IdGrupo = @IdGrupo";
            p.Add("IdGrupo", IdGrupo);
        }

        // Resumen agrupado por grupo
        var sql = $@"SELECT
            ISNULL(g.Grupo1, 'Sin Grupo') AS Grupo,
            COUNT(*) AS CantidadPiezas,
            ISNULL(SUM(p.Peso), 0) AS TotalPeso,
            ISNULL(SUM(p.CBTotal), 0) AS TotalCBTotal,
            ISNULL(SUM(p.CNTotal), 0) AS TotalCNTotal,
            ISNULL(SUM(CAST(p.Precio AS DECIMAL(18,2))), 0) AS TotalPrecio
        FROM piezas p
        LEFT JOIN grupos g ON g.IdGrupo = p.IdGrupo
        {where}
        GROUP BY g.Grupo1
        ORDER BY g.Grupo1";

        return Task.FromResult<(string, object?)>((sql, p));
    }

    protected override string BuildFilterDescription()
    {
        if (IdGrupo.HasValue) return $"Grupo: {IdGrupo}";
        return "";
    }

    protected override async Task<List<ReportFilterDef>> GetFiltersAsync()
    {
        var grupos = await _catalogService.ObtenerGruposAsync();

        return new List<ReportFilterDef>
        {
            ReportFilterDef.SelectFilter("IdGrupo", "Grupo",
                grupos.Select(g => new ReportFilterOption
                {
                    Value = g.IdGrupo.ToString(),
                    Label = g.Grupo1
                }).ToList(),
                IdGrupo?.ToString()),
        };
    }
}
