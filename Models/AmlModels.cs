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
}
