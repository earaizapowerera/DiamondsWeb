namespace DiamondsWeb.Models;

/// <summary>
/// Resumen de acumulado de un cliente en un período de 6 meses (anti-lavado)
/// </summary>
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
}

/// <summary>
/// Detalle de una nota/venta individual
/// </summary>
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

/// <summary>
/// Configuración de umbrales UMAS para anti-lavado
/// </summary>
public class AmlConfig
{
    /// <summary>Valor de la UMA vigente en pesos</summary>
    public decimal ValorUMA { get; set; } = 117.31m;

    /// <summary>Año de la UMA vigente</summary>
    public int AnioUMA { get; set; } = 2026;

    /// <summary>UMAS para umbral de identificación</summary>
    public decimal UmasIdentificacion { get; set; } = 805m;

    /// <summary>UMAS para umbral de aviso al SAT</summary>
    public decimal UmasAvisoSAT { get; set; } = 1605m;

    /// <summary>UMAS para restricción de efectivo</summary>
    public decimal UmasRestriccionEfectivo { get; set; } = 3210m;

    /// <summary>Período de acumulación en meses</summary>
    public int MesesAcumulacion { get; set; } = 6;

    /// <summary>Monto en pesos para umbral de identificación</summary>
    public decimal MontoIdentificacion => ValorUMA * UmasIdentificacion;

    /// <summary>Monto en pesos para umbral de aviso al SAT</summary>
    public decimal MontoAvisoSAT => ValorUMA * UmasAvisoSAT;

    /// <summary>Monto en pesos para restricción de efectivo</summary>
    public decimal MontoRestriccionEfectivo => ValorUMA * UmasRestriccionEfectivo;
}

/// <summary>
/// Filtros para la pantalla de consulta
/// </summary>
public class AmlFiltros
{
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public string? BuscarCliente { get; set; }
    public string? NivelAlerta { get; set; }
    public string AgrupadorCliente { get; set; } = "NombreCliente";
}

/// <summary>
/// Histórico de valores de UMA por año
/// </summary>
public class UmaHistorico
{
    public int Anio { get; set; }
    public decimal ValorDiario { get; set; }
    public DateTime VigenciaDesde { get; set; }
}
