namespace DiamondsWeb.Models;

/// <summary>
/// Pieza en consignación — representa una pieza de una remisión con Consignacion=1
/// </summary>
public class PiezaConsignacion
{
    public string CodigoBarras { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int IdRemision { get; set; }
    public string Remision { get; set; } = string.Empty;
    public string NombreProveedor { get; set; } = string.Empty;
    public DateTime FechaRemision { get; set; }
    public decimal Peso { get; set; }
    public decimal CBTotal { get; set; }
    public string Kilates { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string NombreStatus { get; set; } = string.Empty;
}

/// <summary>
/// Resumen de una remisión de consignación para la vista agrupada
/// </summary>
public class RemisionConsignacionResumen
{
    public int IdRemision { get; set; }
    public string Remision { get; set; } = string.Empty;
    public string NombreProveedor { get; set; } = string.Empty;
    public DateTime FechaRemision { get; set; }
    public int TotalPiezas { get; set; }
    public decimal MontoTotal { get; set; }
}

/// <summary>
/// Estadísticas de consignación para el dashboard
/// </summary>
public class ConsignacionStats
{
    public int PiezasEnExistencia { get; set; }
    public decimal MontoEnExistencia { get; set; }
    public int PiezasPorDevolver { get; set; }
    public decimal MontoPorDevolver { get; set; }
    public int PiezasDevueltas { get; set; }
    public decimal MontoDevueltas { get; set; }
    public int TotalRemisiones { get; set; }
}
