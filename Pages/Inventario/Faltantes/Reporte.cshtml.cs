using Dapper;
using DiamondsWeb.Models.Reporting;
using DiamondsWeb.Pages.Shared;
using DiamondsWeb.Services;
using DiamondsWeb.Services.Reporting;
using Microsoft.AspNetCore.Mvc;

namespace DiamondsWeb.Pages.Inventario.Faltantes;

/// <summary>
/// Reporte de Faltantes de Inventario.
/// VB6 equivalent: frmRegistroExistencias.frm → ImprimirDB.
/// Muestra piezas que están en sistema pero no se encontraron en inventario físico.
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
    [BindProperty(SupportsGet = true)] public string? Buscar { get; set; }

    protected override string GetTitle() => "Reporte de Faltantes";

    protected override string? GetBackUrl() => "/Inventario/Faltantes";

    protected override List<ReportColumn> GetColumns() => new()
    {
        ReportColumn.Text("CodigoBarras", "Codigo", 1.2f),
        ReportColumn.Text("Descripcion", "Descripcion", 2.5f),
        ReportColumn.Text("Grupo", "Grupo", 0.8f),
        ReportColumn.Text("NombreProveedor", "Proveedor", 1.2f),
        ReportColumn.Number("Peso", "Peso", "N2", 0.6f),
        ReportColumn.Currency("CBTotal", "CB Total", "C2", 0.8f),
        ReportColumn.Currency("CNTotal", "CN Total", "C2", 0.8f),
        ReportColumn.Currency("Precio", "Precio", "C0", 0.8f),
        ReportColumn.Date("FechaCaptura", "Alta", "dd/MM/yyyy", 0.8f),
    };

    protected override Task<(string sql, object? parameters)> BuildQueryAsync()
    {
        var where = "WHERE p.IdStatus = 1"; // Solo activas
        var p = new DynamicParameters();

        if (IdGrupo.HasValue)
        {
            where += " AND p.IdGrupo = @IdGrupo";
            p.Add("IdGrupo", IdGrupo);
        }
        if (!string.IsNullOrWhiteSpace(Buscar))
        {
            where += " AND (p.CodigoBarras LIKE @Buscar OR p.Descripcion LIKE @Buscar)";
            p.Add("Buscar", $"%{Buscar}%");
        }

        // Piezas activas sin conteo reciente en inventario físico
        var sql = $@"SELECT TOP 5000
            p.CodigoBarras, p.Descripcion,
            ISNULL(g.Grupo1, '') AS Grupo,
            ISNULL(pr.NombreProveedor, '') AS NombreProveedor,
            p.Peso, p.CBTotal, p.CNTotal, p.Precio,
            p.FechaCaptura
        FROM piezas p
        LEFT JOIN grupos g ON g.IdGrupo = p.IdGrupo
        LEFT JOIN vProveedores pr ON pr.Proveedor = p.Proveedor
        {where}
          AND NOT EXISTS (
            SELECT 1 FROM InventarioFisico inv
            WHERE inv.CodigoBarras = p.CodigoBarras
              AND inv.Encontrado = 1
              AND inv.IdPeriodo = (SELECT MAX(IdPeriodo) FROM InventarioFisico)
          )
        ORDER BY p.FechaCaptura DESC";

        return Task.FromResult<(string, object?)>((sql, p));
    }

    protected override string BuildFilterDescription()
    {
        var parts = new List<string>();
        if (IdGrupo.HasValue) parts.Add($"Grupo: {IdGrupo}");
        if (!string.IsNullOrWhiteSpace(Buscar)) parts.Add($"Buscar: {Buscar}");
        return string.Join(" | ", parts);
    }

    protected override async Task<List<ReportFilterDef>> GetFiltersAsync()
    {
        var grupos = await _catalogService.ObtenerGruposAsync();

        return new List<ReportFilterDef>
        {
            ReportFilterDef.TextFilter("Buscar", "Buscar", Buscar, "Codigo, descripcion..."),
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
