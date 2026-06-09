namespace DiamondsWeb.Models;

/// <summary>
/// Pieza del catálogo de repetidas — origen: tabla catalogorepetidas
/// </summary>
public class CatalogoRepetida
{
    public string CodigoBarras { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public int Proveedor { get; set; }
    public int? IdGrupo { get; set; }
    public short? Kilates { get; set; }
    public int Precio { get; set; }
    public int? IdDivisor { get; set; }
}

/// <summary>
/// Registro de pieza en lote — origen: tabla LotesRepetidas + vista vLotesRepetidas
/// </summary>
public class LoteRepetidaItem
{
    public string Descripcion { get; set; } = "";
    public int IdLote { get; set; }
    public string CodigoBarras { get; set; } = "";
    public int? IdRemision { get; set; }
    public int? IdFactura { get; set; }
    public int Cantidad { get; set; }
    public decimal Peso { get; set; }
    public decimal PrecioGramo { get; set; }
    public decimal CostoBruto { get; set; }
    public decimal? Descuento { get; set; }
    public decimal CostoNeto { get; set; }
    public int? IdMoneda { get; set; }
    public DateTime FechaCaptura { get; set; }
    public DateTime? FechaUltEdicion { get; set; }
    public int IdUsuario { get; set; }
    public int IdTienda { get; set; }
    public int IdLocalizacion { get; set; }
    public decimal? TCCosto { get; set; }
    public decimal? TCCotizacion { get; set; }
    public int? Precio { get; set; }
    public string? Nombre { get; set; }
}

/// <summary>
/// Moneda — origen: tabla Monedas
/// </summary>
public class Moneda
{
    public int IdMoneda { get; set; }
    public string NombreMoneda { get; set; } = "";
    public bool Extranjera { get; set; }
}

/// <summary>
/// Tipo de cambio — origen: tabla tiposcambio
/// </summary>
public class TipoCambio
{
    public int IdTipoCambio { get; set; }
    public int IdMoneda { get; set; }
    public decimal TipoCambioCotizacion { get; set; }
    public decimal? TipoCambioVenta { get; set; }
}

/// <summary>
/// Proveedor con defaults — origen: vista vProveedores
/// </summary>
public class ProveedorConDefaults
{
    public int Proveedor { get; set; }
    public string NombreProveedor { get; set; } = "";
    public int? IdMoneda { get; set; }
    public string? Moneda { get; set; }
    public bool UtilidadExtra { get; set; }
    public string? CaracteristicaDefault { get; set; }
    public string? CostoDefault { get; set; }
    public decimal? DefaultUtilidadOro { get; set; }
    public decimal? DefaultUtilidadGemas { get; set; }
    public decimal? DefaultUtilidadReloj { get; set; }
    public decimal? DefaultUtilidadExtra { get; set; }
    public string? DefaultUtilidad { get; set; }
    public bool UtilizarMoneda { get; set; }
}

/// <summary>
/// Remisión — origen: tabla Remisiones
/// </summary>
public class Remision
{
    public int IdRemision { get; set; }
    public int Proveedor { get; set; }
    public string? NumRemision { get; set; }
    public DateTime? FechaRemision { get; set; }
    public bool Consignacion { get; set; }
    public string? NombreProveedor { get; set; }
}

/// <summary>
/// Factura — origen: tabla Facturas
/// </summary>
public class Factura
{
    public int IdFactura { get; set; }
    public string? FolioFactura { get; set; }
    public int Proveedor { get; set; }
    public int? IdRazonSocialProveedor { get; set; }
    public DateTime? FechaFactura { get; set; }
    public string? NombreProveedor { get; set; }
}

/// <summary>
/// Defaults de impuesto/divisor — origen: tabla defaultsfactorcomunes
/// </summary>
public class DefaultsFactorComunes
{
    public decimal DefaultImpuesto { get; set; }
    public decimal DefaultDivisor { get; set; }
}

/// <summary>
/// Rango de utilidad extra por precio/gramo — origen: tabla utilidadextra_preciogramo
/// </summary>
public class UtilidadExtraPrecioGramo
{
    public int Id { get; set; }
    public decimal PrecioGramoDesde { get; set; }
    public decimal PrecioGramoHasta { get; set; }
    public decimal DefaultUtilidadExtra { get; set; }
}

/// <summary>
/// Razón social de proveedor para dropdown (simplificado)
/// La entidad completa está en ProveedorModels.cs
/// </summary>
public class RazonSocialProveedorItem
{
    public int IdRazonSocialProveedor { get; set; }
    public string RazonSocial { get; set; } = "";
}

/// <summary>
/// DTO para crear una nueva pieza en lote
/// </summary>
public class CrearLoteRepetidaRequest
{
    public string CodigoBarras { get; set; } = "";
    public int? IdRemision { get; set; }
    public int? IdFactura { get; set; }
    public int Cantidad { get; set; }
    public decimal Peso { get; set; }
    public decimal PrecioGramo { get; set; }
    public decimal CostoBruto { get; set; }
    public decimal Descuento { get; set; }
    public decimal CostoNeto { get; set; }
    public int IdMoneda { get; set; }
    public decimal TCCosto { get; set; }
    public decimal TCCotizacion { get; set; }
}
