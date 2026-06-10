namespace DiamondsWeb.Models;

/// <summary>
/// Nota de venta (vista vbajasnotas)
/// </summary>
public class NotaVenta
{
    public int IdNota { get; set; }
    public string NombreCliente { get; set; } = string.Empty;
    public string? Telefonos { get; set; }
    public decimal Bruto { get; set; }
    public decimal Descuento { get; set; }
    public decimal Neto { get; set; }
    public int IdUsuario { get; set; }
    public string? FormaPago { get; set; }
    public DateTime? FechaCaptura { get; set; }
    public DateTime? FechaBaja { get; set; }
    public string? Comentarios { get; set; }
    public decimal? Total { get; set; }
    public int CantidadPiezas { get; set; }
}

/// <summary>
/// Pieza dentro de una nota (piezasnotas + vbajaspiezas para detalle)
/// </summary>
public class PiezaNota
{
    public int IdPiezaNota { get; set; }
    public int IdNota { get; set; }
    public string CodigoBarras { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int? Cantidad { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public DateTime? FechaBaja { get; set; }
    // Campos de vbajaspiezas
    public string? Proveedor { get; set; }
    public decimal? CNTotal { get; set; }
    public int? IdMoneda { get; set; }
    public decimal? Precio { get; set; }
    public string? NombreProveedor => Proveedor;
}

/// <summary>
/// Pago de una nota (bajaspagosnotas + opcionespago)
/// </summary>
public class PagoNota
{
    public int IdNota { get; set; }
    public int IdOpcionPago { get; set; }
    public string? OpcionPago { get; set; }
    public decimal Importe { get; set; }
    public decimal? TipoCambio { get; set; }
    public decimal? ImporteOriginal { get; set; }
    public DateTime? FechaCaptura { get; set; }
}

/// <summary>
/// Totales de costo neto por moneda
/// </summary>
public class CostoNetoPorMoneda
{
    public decimal CostoNeto { get; set; }
    public string Moneda { get; set; } = string.Empty;
}

/// <summary>
/// Filtros de busqueda para notas
/// </summary>
public class NotasFiltro
{
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public string? NombreCliente { get; set; }
    // Filtros de pieza (subquery)
    public string? CodigoBarras { get; set; }
    public string? Proveedor { get; set; }
    public string? DescripcionPieza { get; set; }
    public string? Grupo { get; set; }
    public string? IdLocalizacion { get; set; }
    public string? Modelo { get; set; }
    public string? Serie { get; set; }
    public decimal? PesoDesde { get; set; }
    public decimal? PesoHasta { get; set; }
    public decimal? QuilatesDesde { get; set; }
    public decimal? QuilatesHasta { get; set; }
}
