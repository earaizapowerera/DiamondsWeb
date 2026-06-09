namespace DiamondsWeb.Models;

/// <summary>
/// Pieza desde vista vactualizapiezas — usada en pantalla de Actualización Pieza por Pieza
/// </summary>
public class PiezaActualizacion
{
    public string CodigoBarras { get; set; } = "";
    public string? Obs2 { get; set; }
    public int? IdFactura { get; set; }
    public int? IdRemision { get; set; }
    public string? Remision { get; set; }
    public int? Proveedor { get; set; }
    public string? Descripcion { get; set; }
    public DateTime? FechaCaptura { get; set; }
    public decimal? TCCosto { get; set; }
    public decimal? CBPieza { get; set; }
    public decimal? CNPieza { get; set; }
    public decimal? DescPieza { get; set; }
    public decimal? CostoMN { get; set; }      // CNFactura
    public int? IdMoneda { get; set; }
    public decimal? CostoBrutoMN { get; set; }  // CBFactura
}

/// <summary>
/// Factura desde vista vBuscaFacturas
/// </summary>
public class FacturaBusqueda
{
    public int IdFactura { get; set; }
    public string? FolioFactura { get; set; }
    public DateTime? FechaFactura { get; set; }
    public DateTime? FechaCaptura { get; set; }
    public DateTime? FechaUltEdicion { get; set; }
    public int? IdUsuario { get; set; }
    public string? RazonSocialProveedor { get; set; }
    public int? IdRazonSocialProveedor { get; set; }
    public int? Proveedor { get; set; }
    public int? IdTienda { get; set; }
}

/// <summary>
/// Moneda para dropdown de selección de divisa
/// </summary>
public class MonedaCatalogo
{
    public int IdMoneda { get; set; }
    public string Moneda { get; set; } = "";
    public bool Extranjera { get; set; }
}

/// <summary>
/// Razón social de proveedor para dropdown de alta factura
/// </summary>
public class RazonSocialCatalogo
{
    public int IdRazonSocialProveedor { get; set; }
    public string? RazonSocialProveedor { get; set; }
    public int? Proveedor { get; set; }
}

/// <summary>
/// DTO para actualización de costos de pieza (el UPDATE final)
/// </summary>
public class ActualizarCostoPiezaDto
{
    public string CodigoBarras { get; set; } = "";
    public int IdFactura { get; set; }
    public decimal CBPieza { get; set; }
    public decimal CNPieza { get; set; }
    public int IdMoneda { get; set; }
    public decimal TCCosto { get; set; }
    public decimal CBFactura { get; set; }
    public decimal CNFactura { get; set; }
    public decimal DescFactura { get; set; }
}
