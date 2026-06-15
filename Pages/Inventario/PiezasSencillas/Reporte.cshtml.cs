using Dapper;
using DiamondsWeb.Models.Reporting;
using DiamondsWeb.Pages.Shared;
using DiamondsWeb.Services;
using DiamondsWeb.Services.Reporting;
using Microsoft.AspNetCore.Mvc;

namespace DiamondsWeb.Pages.Inventario.PiezasSencillas;

/// <summary>
/// Reporte de Piezas Sencillas.
/// VB6 equivalent: frmSencillas.frm → ImprimirDB con query a vpiezas
/// y totales: sum(Peso, CBTotal, CNTotal, CBPieza, CNPieza, Precio).
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

    [BindProperty(SupportsGet = true)] public string? Buscar { get; set; }
    [BindProperty(SupportsGet = true)] public int? IdGrupo { get; set; }
    [BindProperty(SupportsGet = true)] public int? Proveedor { get; set; }

    protected override string GetTitle() => "Listado de Piezas Sencillas";

    protected override string? GetBackUrl() => "/Inventario/PiezasSencillas";

    protected override List<ReportColumn> GetColumns() => new()
    {
        ReportColumn.Text("CodigoBarras", "Codigo", 1.2f),
        ReportColumn.Text("Descripcion", "Descripcion", 2.5f),
        ReportColumn.Text("Grupo", "Grupo", 0.8f),
        ReportColumn.Text("NombreProveedor", "Proveedor", 1.2f),
        ReportColumn.Number("Peso", "Peso", "N2", 0.7f),
        ReportColumn.Currency("CBPieza", "CB Pieza", "C2", 0.8f),
        ReportColumn.Currency("CNPieza", "CN Pieza", "C2", 0.8f),
        ReportColumn.Currency("CBTotal", "CB Total", "C2", 0.8f),
        ReportColumn.Currency("CNTotal", "CN Total", "C2", 0.8f),
        ReportColumn.Currency("Precio", "Precio", "C0", 0.8f),
        ReportColumn.Text("Kilates", "Kilates", 0.5f),
        ReportColumn.Text("StatusNombre", "Status", 0.7f),
        ReportColumn.Date("FechaCaptura", "Fecha", "dd/MM/yyyy", 0.8f),
    };

    protected override async Task<(string sql, object? parameters)> BuildQueryAsync()
    {
        var where = "WHERE 1=1";
        var p = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(Buscar))
        {
            where += " AND (p.CodigoBarras LIKE @Buscar OR p.Descripcion LIKE @Buscar" +
                     " OR p.Modelo LIKE @Buscar OR p.NumSerie LIKE @Buscar)";
            p.Add("Buscar", $"%{Buscar}%");
        }
        if (IdGrupo.HasValue)
        {
            where += " AND p.IdGrupo = @IdGrupo";
            p.Add("IdGrupo", IdGrupo);
        }
        if (Proveedor.HasValue)
        {
            where += " AND p.Proveedor = @Proveedor";
            p.Add("Proveedor", Proveedor);
        }

        // VB6: "SELECT * FROM vpiezas" + QueryLocal
        // StatusNombre se calcula con CASE porque no existe en la vista
        var sql = $@"SELECT TOP 5000
            p.CodigoBarras, p.Descripcion, g.Grupo1 AS Grupo,
            pr.NombreProveedor, p.Peso,
            p.CBPieza, p.CNPieza, p.CBTotal, p.CNTotal,
            p.Precio, p.Kilates,
            CASE p.IdStatus WHEN 1 THEN 'Activa' WHEN 2 THEN 'Vendida' WHEN 3 THEN 'Baja' ELSE '' END AS StatusNombre,
            p.FechaCaptura
        FROM piezas p
        LEFT JOIN vProveedores pr ON pr.Proveedor = p.Proveedor
        LEFT JOIN grupos g ON g.IdGrupo = p.IdGrupo
        {where}
        ORDER BY p.FechaCaptura DESC";

        return (sql, p);
    }

    protected override string BuildFilterDescription()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(Buscar)) parts.Add($"Buscar: {Buscar}");
        if (IdGrupo.HasValue) parts.Add($"Grupo: {IdGrupo}");
        if (Proveedor.HasValue) parts.Add($"Proveedor: {Proveedor}");
        return string.Join(" | ", parts);
    }

    protected override async Task<List<ReportFilterDef>> GetFiltersAsync()
    {
        var grupos = await _catalogService.ObtenerGruposAsync();
        var proveedores = await _catalogService.ObtenerProveedoresAsync();

        return new List<ReportFilterDef>
        {
            ReportFilterDef.TextFilter("Buscar", "Buscar", Buscar,
                "Descripcion, codigo, modelo, serie..."),
            ReportFilterDef.SelectFilter("IdGrupo", "Grupo",
                grupos.Select(g => new ReportFilterOption
                {
                    Value = g.IdGrupo.ToString(),
                    Label = g.Grupo1
                }).ToList(),
                IdGrupo?.ToString()),
            ReportFilterDef.SelectFilter("Proveedor", "Proveedor",
                proveedores.Select(p => new ReportFilterOption
                {
                    Value = p.Proveedor1.ToString(),
                    Label = p.NombreProveedor
                }).ToList(),
                Proveedor?.ToString()),
        };
    }
}
