namespace DiamondsWeb.Models;

public class ClienteAmlResumen
{
    public string NombreCliente { get; set; } = string.Empty;
    public string? RFC { get; set; }
    public string? Telefonos { get; set; }
    public decimal TotalAcumulado { get; set; }
    public int NumeroOperaciones { get; set; }
    public DateTime PrimeraOperacion { get; set; }
    public DateTime UltimaOperacion { get; set; }
    public string NivelAlerta { get; set; } = "Normal";
    public bool RequiereIdentificacion { get; set; }
    public bool RequiereAvisoSAT { get; set; }
    public bool YaReportado { get; set; }
    public DateTime? FechaReportePrevio { get; set; }
    public string? ReportadoPor { get; set; }
    public string? NombreArchivoXml { get; set; }
    public DateTime? FechaGeneracionXml { get; set; }
}

public class PagoDetalle
{
    public string OpcionPago { get; set; } = string.Empty;
    public decimal Importe { get; set; }
}

public class NotaDetalle
{
    public int IdNota { get; set; }
    public string NombreCliente { get; set; } = string.Empty;
    public string? RFC { get; set; }
    public string? Telefonos { get; set; }
    public decimal Total { get; set; }
    public DateTime FechaBaja { get; set; }
    public string? FormaPago { get; set; }
}

public class ClienteReportado
{
    public int Id { get; set; }
    public string NombreCliente { get; set; } = string.Empty;
    public string? RFC { get; set; }
    public string? Telefonos { get; set; }
    public int MesReporte { get; set; }
    public int AnioReporte { get; set; }
    public decimal TotalAcumulado { get; set; }
    public int NumeroOperaciones { get; set; }
    public string NivelAlerta { get; set; } = string.Empty;
    public DateTime FechaReporte { get; set; }
    public string? ReportadoPor { get; set; }
    public string? Observaciones { get; set; }
    public string? NombreArchivoXml { get; set; }
    public DateTime? FechaGeneracionXml { get; set; }
}

public class AmlIdentificacion
{
    public int Id { get; set; }
    public string NombreCliente { get; set; } = string.Empty;
    public string TipoDocumento { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public string? ContentType { get; set; }
    public long FileSize { get; set; }
    public string? SubidoPor { get; set; }
    public DateTime FechaSubida { get; set; }
    public string? Notas { get; set; }
    public int? FotoId { get; set; }
    public string Url => $"/aml-identificaciones/{StoredFileName}";
}

public class AmlConfig
{
    public decimal ValorUMA { get; set; } = 117.31m;
    public int AnioUMA { get; set; } = 2026;
    public decimal UmasIdentificacion { get; set; } = 805m;
    public decimal UmasAvisoSAT { get; set; } = 1605m;
    public decimal UmasRestriccionEfectivo { get; set; } = 3210m;
    public int MesesAcumulacion { get; set; } = 6;

    public decimal MontoIdentificacion => ValorUMA * UmasIdentificacion;
    public decimal MontoAvisoSAT => ValorUMA * UmasAvisoSAT;
    public decimal MontoRestriccionEfectivo => ValorUMA * UmasRestriccionEfectivo;

    /// <summary>
    /// Tabla de valores UMA por año (fuente: INEGI).
    /// La nueva UMA entra en vigor el 1 de febrero de cada año.
    /// </summary>
    private static readonly Dictionary<int, decimal> UmasPorAnio = new()
    {
        { 2016, 73.04m },
        { 2017, 75.49m },
        { 2018, 80.60m },
        { 2019, 84.49m },
        { 2020, 86.88m },
        { 2021, 89.62m },
        { 2022, 96.22m },
        { 2023, 103.74m },
        { 2024, 108.57m },
        { 2025, 113.14m },
        { 2026, 117.31m },
    };

    /// <summary>
    /// Obtiene una copia del config con el UMA vigente para un mes/año dado.
    /// Enero usa UMA del año anterior (entra en vigor 1 de febrero).
    /// </summary>
    public AmlConfig ParaMesAnio(int mes, int anio)
    {
        int anioUma = mes == 1 ? anio - 1 : anio;

        // Buscar el UMA del año, o usar el más reciente disponible
        decimal valorUma;
        if (UmasPorAnio.TryGetValue(anioUma, out var uma))
            valorUma = uma;
        else if (anioUma > UmasPorAnio.Keys.Max())
            valorUma = UmasPorAnio[UmasPorAnio.Keys.Max()];
        else
            valorUma = UmasPorAnio[UmasPorAnio.Keys.Min()];

        return new AmlConfig
        {
            ValorUMA = valorUma,
            AnioUMA = anioUma,
            UmasIdentificacion = UmasIdentificacion,
            UmasAvisoSAT = UmasAvisoSAT,
            UmasRestriccionEfectivo = UmasRestriccionEfectivo,
            MesesAcumulacion = MesesAcumulacion,
        };
    }
}
