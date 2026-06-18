namespace DiamondsWeb.Models.Reporting;

/// <summary>
/// Modelo para la vista parcial _ReportPartial.cshtml.
/// Contiene los datos del reporte, filtros y URLs para exportación.
/// </summary>
public class ReportPartialModel
{
    /// <summary>Datos del reporte (filas, columnas, totales).</summary>
    public ReportResult Report { get; set; } = new();

    /// <summary>Filtros configurados para la UI.</summary>
    public List<ReportFilterDef> Filters { get; set; } = new();

    /// <summary>URL de la página de reporte (para limpiar filtros).</summary>
    public string ReportPageUrl { get; set; } = "";

    /// <summary>URL del handler de Excel (GET con query string de filtros).</summary>
    public string? ExcelHandlerUrl { get; set; }

    /// <summary>URL del handler de PDF (GET con query string de filtros).</summary>
    public string? PdfHandlerUrl { get; set; }

    /// <summary>URL del botón "Volver" (opcional).</summary>
    public string? BackUrl { get; set; }

    /// <summary>Mensaje de error (opcional).</summary>
    public string? ErrorMessage { get; set; }
}
