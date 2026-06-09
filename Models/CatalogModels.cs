namespace DiamondsWeb.Models;

// ── Grupos ──
public class Grupo
{
    public int IdGrupo { get; set; }
    public string Grupo1 { get; set; } = "";
    public int? IdUsuario { get; set; }
    public DateTime? FechaCaptura { get; set; }
}

// ── Monedas ──
public class Moneda
{
    public int IdMoneda { get; set; }
    public string Moneda1 { get; set; } = "";
    public bool Extranjera { get; set; }
    public int? IdUsuario { get; set; }
    public DateTime? FechaCaptura { get; set; }
}

// ── Tipos de Cambio ──
public class TipoCambio
{
    public int IdTipoCambio { get; set; }
    public int IdMoneda { get; set; }
    public string? Moneda { get; set; }
    public decimal TipoCambioCotizacion { get; set; }
    public decimal TipoCambioVenta { get; set; }
    public int? IdUsuario { get; set; }
    public DateTime? FechaCaptura { get; set; }
}

// ── Opciones de Pago ──
public class OpcionPago
{
    public int IdOpcionPago { get; set; }
    public string OpcionPago1 { get; set; } = "";
    public int? IdMoneda { get; set; }
    public string? Moneda { get; set; }
    public int? IdLogo { get; set; }
    public bool Activo { get; set; } = true;
    public int? IdUsuario { get; set; }
    public DateTime? FechaCaptura { get; set; }
}

// ── Divisores (Multiplicadores) ──
public class Divisor
{
    public int IdDivisor { get; set; }
    public string Descripcion { get; set; } = "";
    public decimal ValorDivisor { get; set; }
    public decimal Multiplicador => ValorDivisor != 0 ? 1m / ValorDivisor : 0;
}

// ── Proveedores ──
public class Proveedor
{
    public int Proveedor1 { get; set; }
    public string? NombreProveedor { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public string? Contacto { get; set; }
    public int? IdDefaultCaracteristica { get; set; }
    public int? IdDefaultTipoCosto { get; set; }
    public int? IdDefaultUtilidad { get; set; }
    public int? IdMoneda { get; set; }
    public bool MonedaDefault { get; set; }
    public bool UtilidadExtraPrecioGramo { get; set; }
    public int? IdUsuario { get; set; }
    public DateTime? FechaCaptura { get; set; }
}

// ── Razones Sociales Proveedores ──
public class RazonSocialProveedor
{
    public int IdRazonSocialProveedor { get; set; }
    public string RazonSocialProveedor1 { get; set; } = "";
    public string? RFC { get; set; }
    public string? Calle { get; set; }
    public string? Colonia { get; set; }
    public string? CodigoPostal { get; set; }
    public string? Municipio { get; set; }
    public string? Estado { get; set; }
    public int? IdUsuario { get; set; }
    public DateTime? FechaCaptura { get; set; }
    public DateTime? FechaUltEdicion { get; set; }
}

public class RazonSocialProveedorAsignacion
{
    public int IdRazonSocialProveedor { get; set; }
    public int Proveedor { get; set; }
    public string? NombreProveedor { get; set; }
    public string? RazonSocial { get; set; }
}

// ── Catálogo Repetidas ──
public class CatalogoRepetida
{
    public string CodigoBarras { get; set; } = "";
    public string? Descripcion { get; set; }
    public int? Proveedor { get; set; }
    public string? NombreProveedor { get; set; }
    public int? IdGrupo { get; set; }
    public string? Grupo { get; set; }
    public string? Kilates { get; set; }
    public decimal? Precio { get; set; }
    public int? IdDivisor { get; set; }
    public int? IdUsuario { get; set; }
    public DateTime? FechaCaptura { get; set; }
}

// ── Defaults Factor Comunes ──
public class DefaultFactorComun
{
    public int IdDefault { get; set; }
    public decimal DefaultImpuesto { get; set; }
    public decimal DefaultDivisor { get; set; }
    public int? IdUsuario { get; set; }
    public DateTime? FechaCaptura { get; set; }
}

// ── Defaults Utilidad ──
public class DefaultUtilidad
{
    public int IdDefaultUtilidad { get; set; }
    public decimal DefaultUtilidad1 { get; set; }
    public decimal? DefaultUtilidadReloj { get; set; }
    public decimal? DefaultUtilidadGemas { get; set; }
    public int? IdUsuario { get; set; }
    public DateTime? FechaCaptura { get; set; }
}

// ── Defaults Utilidad Extra ──
public class DefaultUtilidadExtra
{
    public int IdDefaultUtilidadExtra { get; set; }
    public decimal DefaultUtilidadExtra1 { get; set; }
    public int? IdUsuario { get; set; }
    public DateTime? FechaCaptura { get; set; }
}

// ── Utilidad Extra por Precio/Gramo ──
public class UtilidadExtraPrecioGramo
{
    public int IdUtilidadExtra { get; set; }
    public decimal PrecioGramoDesde { get; set; }
    public decimal PrecioGramoHasta { get; set; }
    public decimal UtilidadExtra { get; set; }
    public int? IdUsuario { get; set; }
    public DateTime? FechaCaptura { get; set; }
}

// ── Tablas de Jerarquías ──
public class TablaJerarquia
{
    public int IdTablaJerarquia { get; set; }
    public string Descripcion { get; set; } = "";
    public int? IdUsuario { get; set; }
}

public class Jerarquia
{
    public int IdJerarquia { get; set; }
    public int IdTablaJerarquia { get; set; }
    public string? Columna { get; set; }
    public int? Orden { get; set; }
}

// ── Diseño Etiquetas ──
public class DisenioEtiqueta
{
    public int IdDisenio { get; set; }
    public string? Descripcion { get; set; }
    public string? ArchivoEtiqueta { get; set; }
    public string? ArchivoEtiquetaCompuesta { get; set; }
}
