namespace DiamondsWeb.Models;

// ─── Sesión de Venta ───────────────────────────────────────────

/// <summary>
/// Sesión activa en el POS (una fila en tabla Notas mientras está abierta)
/// </summary>
public class NotaSesion
{
    public int IdNota { get; set; }
    public int IdUsuario { get; set; }
    public string NombreUsuario { get; set; } = "";
    public int IdVendedor { get; set; }
    public string? NombreCliente { get; set; }
    public string? Telefonos { get; set; }
    public string? Comentarios { get; set; }
    public bool Factura { get; set; }
    public DateTime? FechaBaja { get; set; }
    public decimal Bruto { get; set; }
    public decimal Descuento { get; set; }
    public decimal Neto { get; set; }
    public decimal Total { get; set; }
    public string? FormaPago { get; set; }
}

// ─── Piezas ────────────────────────────────────────────────────

/// <summary>
/// Pieza en la nota temporal (PiezasNotasTemporal)
/// </summary>
public class PiezaTemporal
{
    public int IdNota { get; set; }
    public string CodigoBarras { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public short Cantidad { get; set; }
    public decimal SubTotal { get; set; }
    public decimal Total { get; set; }
}

/// <summary>
/// Resultado de búsqueda de pieza por código de barras.
/// Unifica piezas sencillas, repetidas y compuestas.
/// </summary>
public class PiezaLookupResult
{
    public string CodigoBarras { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public decimal Precio { get; set; }
    public decimal Divisor { get; set; }
    public string? CBPadre { get; set; }
    /// <summary>Sencilla, Repetida, Compuesta, Componente</summary>
    public string TipoPieza { get; set; } = "";
    // Campos adicionales para descripción detallada (piezas sencillas)
    public string? Kilates { get; set; }
    public string? Modelo { get; set; }
    public string? Linea { get; set; }
    public decimal Quilates { get; set; }
    public string? Color { get; set; }
    public string? Pureza { get; set; }
    public string? Corte { get; set; }
    public string? NumSerie { get; set; }
}

// ─── Pagos ─────────────────────────────────────────────────────

/// <summary>
/// Opción de pago del catálogo OpcionesPago
/// </summary>
public class OpcionPagoPOS
{
    public int IdOpcionPago { get; set; }
    public string OpcionPago { get; set; } = "";
    public int IdMoneda { get; set; }
    public string NombreMoneda { get; set; } = "";
    public bool Extranjera { get; set; }
    public string? Logo { get; set; }
}

/// <summary>
/// Pago registrado en PagosNotas (vista vPagosNotas)
/// </summary>
public class PagoNotaDetalle
{
    public int IdNota { get; set; }
    public int IdOpcionPago { get; set; }
    public string OpcionPago { get; set; } = "";
    public decimal Importe { get; set; }
    public decimal TipoCambio { get; set; }
    public decimal ImporteOriginal { get; set; }
}

/// <summary>
/// Request para registrar un pago
/// </summary>
public class RegistrarPagoRequest
{
    public int IdNota { get; set; }
    public int IdOpcionPago { get; set; }
    public decimal Importe { get; set; }
    public decimal TipoCambio { get; set; }
    public decimal ImporteOriginal { get; set; }
}

// ─── Resumen ───────────────────────────────────────────────────

/// <summary>
/// Resumen de totales de una nota activa
/// </summary>
public class ResumenNota
{
    public decimal SubTotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal SobrePrecio { get; set; }
    public decimal Total { get; set; }
    public decimal TotalFactura { get; set; }
    public decimal TotalPagado { get; set; }
    public decimal Cambio { get; set; }
    public string FormasPago { get; set; } = "";
    public bool EsFactura { get; set; }
}

// ─── Requests ──────────────────────────────────────────────────

public class AgregarPiezaRequest
{
    public int IdNota { get; set; }
    public string CodigoBarras { get; set; } = "";
    public int? Cantidad { get; set; }
    public bool EsFactura { get; set; }
}

public class AplicarDescuentoRequest
{
    public int IdNota { get; set; }
    /// <summary>Porcentaje de descuento (0-20)</summary>
    public decimal Descuento { get; set; }
    public decimal SobrePrecio { get; set; }
    public bool EsFactura { get; set; }
}

public class CerrarNotaRequest
{
    public int IdNota { get; set; }
    public string NombreCliente { get; set; } = "";
    public string? Telefonos { get; set; }
    public string? Comentarios { get; set; }
    public bool Factura { get; set; }
    public DateTime FechaBaja { get; set; }
    public decimal? Descuento { get; set; }
    public decimal? SobrePrecio { get; set; }
    public decimal? Bruto { get; set; }
    public decimal? Neto { get; set; }
    public decimal? Total { get; set; }
    public string? FormaPago { get; set; }
    public int IdVendedor { get; set; }
}

public class CrearSesionRequest
{
    public int IdUsuario { get; set; }
    public DateTime? FechaBaja { get; set; }
}

/// <summary>
/// Datos de una nota ya cerrada (BajasNotas) para impresión
/// </summary>
public class NotaCerrada
{
    public int IdNota { get; set; }
    public string NombreCliente { get; set; } = "";
    public string? Telefonos { get; set; }
    public string? Comentarios { get; set; }
    public bool Factura { get; set; }
    public DateTime? FechaBaja { get; set; }
    public decimal Bruto { get; set; }
    public decimal Descuento { get; set; }
    public decimal Neto { get; set; }
    public decimal Total { get; set; }
    public string? FormaPago { get; set; }
    public int IdUsuario { get; set; }
    public int IdVendedor { get; set; }
    public string? NombreVendedor { get; set; }
    public List<PiezaNotaFinal> Piezas { get; set; } = [];
    public List<PagoNotaDetalle> Pagos { get; set; } = [];
}

/// <summary>
/// Pieza de nota final (PiezasNotas) para impresión
/// </summary>
public class PiezaNotaFinal
{
    public int IdPiezaNota { get; set; }
    public int IdNota { get; set; }
    public string CodigoBarras { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public short Cantidad { get; set; }
    public decimal SubTotal { get; set; }
    public decimal Total { get; set; }
}
