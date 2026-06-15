namespace DiamondsWeb.Models;

// ─── Sesión de Apartado ───────────────────────────────────────

/// <summary>
/// Sesión activa en el POS Apartados (fila en NotasApartado mientras está abierta)
/// </summary>
public class ApartadoSesion
{
    public int IdNota { get; set; }
    public int IdUsuario { get; set; }
    public string NombreUsuario { get; set; } = "";
    public int IdVendedor { get; set; }
    public string? NombreCliente { get; set; }
    public string? Telefonos { get; set; }
    public string? RFC { get; set; }
    public string? Calle { get; set; }
    public string? CodigoPostal { get; set; }
    public string? Colonia { get; set; }
    public string? Ciudad { get; set; }
    public string? Estado { get; set; }
    public string? Municipio { get; set; }
    public string? CodigoBarrasCliente { get; set; }
    public bool Factura { get; set; }
    public decimal Bruto { get; set; }
    public decimal Descuento { get; set; }
    public decimal Neto { get; set; }
    public decimal Total { get; set; }
    public string? FormaPago { get; set; }
}

// ─── Piezas de Apartado ───────────────────────────────────────

/// <summary>
/// Pieza en la nota de apartado (PiezasNotasApartado)
/// </summary>
public class PiezaApartado
{
    public int IdNota { get; set; }
    public string CodigoBarras { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public short Cantidad { get; set; } = 1;
    public decimal SubTotal { get; set; }
    public decimal Total { get; set; }
}

// ─── Pagos de Apartado ────────────────────────────────────────

/// <summary>
/// Pago registrado en PagosNotasApartado
/// </summary>
public class PagoApartadoDetalle
{
    public int IdNota { get; set; }
    public int IdOpcionPago { get; set; }
    public string OpcionPago { get; set; } = "";
    public decimal Importe { get; set; }
    public decimal TipoCambio { get; set; }
    public decimal ImporteOriginal { get; set; }
}

// ─── Resumen de Apartado ──────────────────────────────────────

/// <summary>
/// Resumen de totales de una nota de apartado activa
/// </summary>
public class ResumenApartado
{
    public decimal SubTotal { get; set; }
    public decimal DescuentoPct { get; set; }
    public decimal SobrePrecio { get; set; }
    public decimal Total { get; set; }
    public decimal TotalFactura { get; set; }
    public decimal TotalPagado { get; set; }
    public decimal Cambio { get; set; }
    public string FormasPago { get; set; } = "";
    public bool EsFactura { get; set; }
}

// ─── Requests de Apartado ─────────────────────────────────────

public class CrearApartadoSesionRequest
{
    public int IdUsuario { get; set; }
}

public class AgregarPiezaApartadoRequest
{
    public int IdNota { get; set; }
    public string CodigoBarras { get; set; } = "";
    public int? Cantidad { get; set; }
    public bool EsFactura { get; set; }
}

public class RegistrarPagoApartadoRequest
{
    public int IdNota { get; set; }
    public int IdOpcionPago { get; set; }
    public decimal Importe { get; set; }
    public decimal TipoCambio { get; set; }
    public decimal ImporteOriginal { get; set; }
}

public class ActualizarApartadoNotaReq
{
    public int IdNota { get; set; }
    public string? NombreCliente { get; set; }
    public string? Telefonos { get; set; }
    public string? RFC { get; set; }
    public string? Calle { get; set; }
    public string? CodigoPostal { get; set; }
    public string? Colonia { get; set; }
    public string? Ciudad { get; set; }
    public string? Estado { get; set; }
    public string? Municipio { get; set; }
    public string? CodigoBarrasCliente { get; set; }
    public bool Factura { get; set; }
    public int IdVendedor { get; set; }
}

public class CerrarApartadoRequest
{
    public int IdNota { get; set; }
    public string NombreCliente { get; set; } = "";
    public string? Telefonos { get; set; }
    public bool Factura { get; set; }
    public decimal? Descuento { get; set; }
    public decimal? SobrePrecio { get; set; }
    public decimal? Total { get; set; }
    public string? FormaPago { get; set; }
    public int IdVendedor { get; set; }
}

public class EliminarPiezaApartadoReq
{
    public int IdNota { get; set; }
    public string CodigoBarras { get; set; } = "";
    public decimal DescuentoPct { get; set; }
    public decimal SobrePrecio { get; set; }
    public bool EsFactura { get; set; }
}

public class EliminarPagoApartadoReq
{
    public int IdNota { get; set; }
    public int IdOpcionPago { get; set; }
    public decimal Importe { get; set; }
}

public class CancelarApartadoReq
{
    public int IdNota { get; set; }
}
