namespace DiamondsWeb.Models.Reporting;

/// <summary>
/// Define una columna del reporte con formato y alineación.
/// Reemplaza la detección automática de columnas del DataGrid VB6.
/// </summary>
public class ReportColumn
{
    public string Field { get; set; } = "";
    public string Header { get; set; } = "";
    public string Format { get; set; } = "";
    public ColumnAlign Align { get; set; } = ColumnAlign.Left;
    public float RelativeWidth { get; set; } = 1f;
    public bool IsSummable { get; set; }
    public string ExcelFormat { get; set; } = "";

    /// <summary>Acceso directo: columna de texto alineada a la izquierda.</summary>
    public static ReportColumn Text(string field, string header, float width = 1f) =>
        new() { Field = field, Header = header, RelativeWidth = width };

    /// <summary>Acceso directo: columna numérica alineada a la derecha con suma.</summary>
    public static ReportColumn Number(string field, string header, string format = "N2",
        float width = 0.8f, bool summable = true) =>
        new()
        {
            Field = field, Header = header, Format = format,
            Align = ColumnAlign.Right, RelativeWidth = width,
            IsSummable = summable,
            ExcelFormat = FormatToExcel(format)
        };

    /// <summary>Acceso directo: columna de moneda alineada a la derecha con suma.</summary>
    public static ReportColumn Currency(string field, string header,
        string format = "C2", float width = 0.9f, bool summable = true) =>
        new()
        {
            Field = field, Header = header, Format = format,
            Align = ColumnAlign.Right, RelativeWidth = width,
            IsSummable = summable,
            ExcelFormat = FormatToExcel(format)
        };

    /// <summary>Acceso directo: columna de fecha.</summary>
    public static ReportColumn Date(string field, string header,
        string format = "dd/MM/yyyy", float width = 0.8f) =>
        new()
        {
            Field = field, Header = header, Format = format,
            RelativeWidth = width,
            ExcelFormat = "dd/MM/yyyy"
        };

    private static string FormatToExcel(string format) => format switch
    {
        "N2" => "#,##0.00",
        "N0" => "#,##0",
        "C2" => "$#,##0.00",
        "C0" => "$#,##0",
        _ => ""
    };
}

public enum ColumnAlign { Left, Right, Center }

/// <summary>
/// Definición completa de un reporte: título, columnas y filtros activos.
/// </summary>
public class ReportDefinition
{
    public string Title { get; set; } = "Reporte";
    public string Subtitle { get; set; } = "";
    public List<ReportColumn> Columns { get; set; } = new();
    public string FilterDescription { get; set; } = "";
    public bool LandscapeOrientation { get; set; } = true;
}

/// <summary>
/// Resultado del reporte con datos y totales calculados automáticamente.
/// </summary>
public class ReportResult
{
    public ReportDefinition Definition { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public Dictionary<string, decimal> Totals { get; set; } = new();
    public int TotalRows => Rows.Count;
}

/// <summary>
/// Opciones de filtro para la UI del reporte.
/// </summary>
public class ReportFilterOption
{
    public string Value { get; set; } = "";
    public string Label { get; set; } = "";
}

/// <summary>
/// Definición de un filtro en la UI del reporte.
/// </summary>
public class ReportFilterDef
{
    public string Name { get; set; } = "";
    public string Label { get; set; } = "";
    public FilterType Type { get; set; } = FilterType.Text;
    public string? CurrentValue { get; set; }
    public List<ReportFilterOption> Options { get; set; } = new();
    public string Placeholder { get; set; } = "";

    public static ReportFilterDef TextFilter(string name, string label,
        string? value = null, string placeholder = "") =>
        new() { Name = name, Label = label, Type = FilterType.Text, CurrentValue = value, Placeholder = placeholder };

    public static ReportFilterDef SelectFilter(string name, string label,
        List<ReportFilterOption> options, string? value = null) =>
        new() { Name = name, Label = label, Type = FilterType.Select, Options = options, CurrentValue = value };

    public static ReportFilterDef DateFilter(string name, string label, string? value = null) =>
        new() { Name = name, Label = label, Type = FilterType.Date, CurrentValue = value };
}

public enum FilterType { Text, Select, Date }
