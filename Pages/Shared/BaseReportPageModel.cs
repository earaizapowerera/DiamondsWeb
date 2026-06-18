using DiamondsWeb.Models.Reporting;
using DiamondsWeb.Services.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Shared;

/// <summary>
/// Clase base para páginas de reporte. Maneja los handlers de Excel y PDF automáticamente.
/// Las clases hijas solo definen: columnas, query, filtros.
///
/// Equivalente web del patrón VB6: Form + DataGrid + ImprimirDB.
/// En VB6, cada form tenía su DataGrid y llamaba ImprimirDB(grid, conn, query, params).
/// Aquí, cada página hereda de BaseReportPageModel y define su reporte.
/// </summary>
[Authorize]
public abstract class BaseReportPageModel : PageModel
{
    protected readonly ReportDataBuilder ReportData;
    protected readonly ILogger Logger;

    protected BaseReportPageModel(ReportDataBuilder reportData, ILogger logger)
    {
        ReportData = reportData;
        Logger = logger;
    }

    /// <summary>Resultado del reporte (se llena en OnGetAsync).</summary>
    public ReportResult Report { get; set; } = new();

    /// <summary>Modelo para la vista parcial.</summary>
    public ReportPartialModel PartialModel { get; set; } = new();

    /// <summary>Define las columnas del reporte.</summary>
    protected abstract List<ReportColumn> GetColumns();

    /// <summary>Define el título del reporte.</summary>
    protected abstract string GetTitle();

    /// <summary>Genera el SQL y parámetros para el reporte según los filtros actuales.</summary>
    protected abstract Task<(string sql, object? parameters)> BuildQueryAsync();

    /// <summary>Genera la descripción de filtros para el encabezado.</summary>
    protected abstract string BuildFilterDescription();

    /// <summary>Define los filtros de la UI. Override opcional.</summary>
    protected virtual Task<List<ReportFilterDef>> GetFiltersAsync() =>
        Task.FromResult(new List<ReportFilterDef>());

    /// <summary>URL de la página de reporte (para limpiar filtros).</summary>
    protected virtual string GetReportPageUrl() => HttpContext.Request.Path;

    /// <summary>URL del botón "Volver" (opcional).</summary>
    protected virtual string? GetBackUrl() => null;

    /// <summary>¿Landscape u orientación normal?</summary>
    protected virtual bool IsLandscape() => true;

    /// <summary>Máximo de filas en el reporte.</summary>
    protected virtual int GetMaxRows() => 5000;

    /// <summary>Genera el query string actual para links de exportación.</summary>
    private string CurrentQueryString()
    {
        var qs = HttpContext.Request.QueryString.Value ?? "";
        return qs;
    }

    /// <summary>Construye la definición completa del reporte.</summary>
    private ReportDefinition BuildDefinition() => new()
    {
        Title = GetTitle(),
        Columns = GetColumns(),
        FilterDescription = BuildFilterDescription(),
        LandscapeOrientation = IsLandscape()
    };

    public async Task OnGetAsync()
    {
        try
        {
            var definition = BuildDefinition();
            var (sql, parameters) = await BuildQueryAsync();
            Report = await ReportData.ExecuteAsync(definition, sql, parameters, GetMaxRows());

            var filters = await GetFiltersAsync();
            var qs = CurrentQueryString();
            var pagePath = GetReportPageUrl();

            PartialModel = new ReportPartialModel
            {
                Report = Report,
                Filters = filters,
                ReportPageUrl = pagePath,
                ExcelHandlerUrl = $"{pagePath}?handler=Excel{(qs.Length > 1 ? "&" + qs.TrimStart('?') : "")}",
                PdfHandlerUrl = $"{pagePath}?handler=Pdf{(qs.Length > 1 ? "&" + qs.TrimStart('?') : "")}",
                BackUrl = GetBackUrl()
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error al generar reporte: {Title}", GetTitle());
            PartialModel = new ReportPartialModel
            {
                Report = new ReportResult { Definition = BuildDefinition() },
                Filters = await GetFiltersAsync(),
                ReportPageUrl = GetReportPageUrl(),
                BackUrl = GetBackUrl(),
                ErrorMessage = $"Error al generar reporte: {ex.Message}"
            };
        }
    }

    public async Task<IActionResult> OnGetExcelAsync()
    {
        try
        {
            var definition = BuildDefinition();
            var (sql, parameters) = await BuildQueryAsync();
            var report = await ReportData.ExecuteAsync(definition, sql, parameters, GetMaxRows());
            var bytes = ReportExcelBuilder.Build(report);
            var fileName = $"{GetTitle().Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error al exportar Excel: {Title}", GetTitle());
            TempData["Error"] = $"Error al exportar: {ex.Message}";
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnGetPdfAsync()
    {
        try
        {
            var definition = BuildDefinition();
            var (sql, parameters) = await BuildQueryAsync();
            var report = await ReportData.ExecuteAsync(definition, sql, parameters, GetMaxRows());
            var bytes = ReportPdfBuilder.Build(report);
            var fileName = $"{GetTitle().Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
            return File(bytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error al exportar PDF: {Title}", GetTitle());
            TempData["Error"] = $"Error al exportar PDF: {ex.Message}";
            return RedirectToPage();
        }
    }
}
