namespace DiamondsWeb.Models;

/// <summary>
/// Registro del último corte de caja (tabla corte, 1 sola fila).
/// </summary>
public class CorteActual
{
    public DateTime FechaCorte { get; set; }
}

/// <summary>
/// Entrada en el historial de cortes (tabla cortes_historial).
/// </summary>
public class CorteHistorial
{
    public int Id { get; set; }
    public DateTime FechaCorte { get; set; }
    public DateTime? FechaCorteAnterior { get; set; }
    public int IdUsuario { get; set; }
    public string? NombreUsuario { get; set; }
    public int TotalNotas { get; set; }
    public decimal TotalVentas { get; set; }
    public string? Comentario { get; set; }
    public DateTime FechaRegistro { get; set; }
}

/// <summary>
/// Resumen de ventas entre dos fechas (para el dashboard).
/// </summary>
public class ResumenVentasPeriodo
{
    public int TotalNotas { get; set; }
    public decimal TotalBruto { get; set; }
    public decimal TotalDescuento { get; set; }
    public decimal TotalNeto { get; set; }
    public decimal TotalImpuesto { get; set; }
    public decimal TotalVenta { get; set; }
}

/// <summary>
/// Desglose de ventas por forma de pago.
/// </summary>
public class VentaPorFormaPago
{
    public string FormaPago { get; set; } = string.Empty;
    public int CantidadNotas { get; set; }
    public decimal Total { get; set; }
}
