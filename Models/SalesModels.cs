namespace DiamondsWeb.Models;

// ── Bajas Piezas ──
public class BajaPieza
{
    public string CodigoBarras { get; set; } = "";
    public string? Descripcion { get; set; }
    public decimal? Precio { get; set; }
    public string? NombreProveedor { get; set; }
    public string? Grupo { get; set; }
    public string? NombreCliente { get; set; }
    public DateTime? FechaBaja { get; set; }
    public string IdNota { get; set; } = "";
}

// ── Devoluciones a Proveedor ──
// Tabla: devoluciones (CodigoBarras PK, MotivoDevolucion, Remision, FechaDevolucion, IdUsuario)
// No tiene identity column — CodigoBarras es la clave.
public class Devolucion
{
    public string CodigoBarras { get; set; } = "";
    public string? Descripcion { get; set; }
    public string? MotivoDevolucion { get; set; }
    public string? Remision { get; set; }
    public int? IdUsuario { get; set; }
    public DateTime? FechaDevolucion { get; set; }
}

public class DevolucionCliente
{
    public string CodigoBarras { get; set; } = "";
    public string? NombreCliente { get; set; }
    public DateTime? FechaCompra { get; set; }
    public string? Descripcion { get; set; }
    public decimal? Precio { get; set; }
    public decimal? Descuento { get; set; }
    public decimal? PrecioPagado { get; set; }
    public string? Tienda { get; set; }
    public string? FormaPago { get; set; }
    public bool YaReestablecida { get; set; }
}

// ── Consignación ──
public class ConsignacionItem
{
    public string CodigoBarras { get; set; } = "";
    public string? Descripcion { get; set; }
    public decimal? Precio { get; set; }
    public string? NombreProveedor { get; set; }
    public int? IdRemision { get; set; }
    public string? Estado { get; set; }
    public DateTime? FechaConsignacion { get; set; }
}

public class PiezaActualizable
{
    public string CodigoBarras { get; set; } = "";
    public string? Descripcion { get; set; }
    public decimal? Precio { get; set; }
    public string? NombreProveedor { get; set; }
    public string? IdFactura { get; set; }
    public string? IdRemision { get; set; }
    public decimal? CBPieza { get; set; }
    public decimal? CNPieza { get; set; }
    public decimal? CBFactura { get; set; }
    public decimal? CNFactura { get; set; }
    public decimal? TCCosto { get; set; }
    public int? IdMoneda { get; set; }
}

// ── POS: Sesión de Venta ──
public class SesionVenta
{
    public string IdNota { get; set; } = "";
    public int? IdUsuario { get; set; }
    public DateTime? FechaCreacion { get; set; }
}

public class PiezaNotaTemporal
{
    public string IdNota { get; set; } = "";
    public string CodigoBarras { get; set; } = "";
    public string? Descripcion { get; set; }
    public decimal SubTotal { get; set; }
    public int Cantidad { get; set; } = 1;
    public decimal Total { get; set; }
}

public class PagoNotaTemporal
{
    public string IdNota { get; set; } = "";
    public int IdOpcionPago { get; set; }
    public string? NombreOpcionPago { get; set; }
    public decimal Importe { get; set; }
    public decimal? ImporteOriginal { get; set; }
    public decimal? TipoCambio { get; set; }
}

// ── Localizaciones ──
public class Localizacion
{
    public int IdLocalizacion { get; set; }
    public string? NombreLocalizacion { get; set; }
    public int? IdTienda { get; set; }
}
