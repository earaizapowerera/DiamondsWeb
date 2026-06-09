namespace DiamondsWeb.Models;

/// <summary>
/// Factura de proveedor — tabla facturas
/// </summary>
public class FacturaDto
{
    public int IdFactura { get; set; }
    public string FolioFactura { get; set; } = string.Empty;
    public int? Proveedor { get; set; }
    public string? NombreProveedor { get; set; }
    public int IdRazonSocialProveedor { get; set; }
    public string? RazonSocialProveedor { get; set; }
    public DateTime FechaFactura { get; set; }
    public DateTime? FechaCaptura { get; set; }
    public DateTime? FechaUltEdicion { get; set; }
    public int IdUsuario { get; set; }
    public int? IdTienda { get; set; }
    public string? Pedimento { get; set; }
}

/// <summary>
/// Pieza disponible para vincular a factura (vista vActualizaPiezas filtrada)
/// </summary>
public class PiezaDisponibleDto
{
    public string CodigoBarras { get; set; } = string.Empty;
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
    public decimal? CostoMN { get; set; }
    public decimal? CostoBrutoMN { get; set; }
    public int? IdMoneda { get; set; }
}

/// <summary>
/// Pieza ya vinculada a una factura (grid derecho)
/// </summary>
public class PiezaVinculadaDto
{
    public string CodigoBarras { get; set; } = string.Empty;
    public string? Obs2 { get; set; }
    public decimal? CBFactura { get; set; }
    public decimal? CNFactura { get; set; }
    public decimal? TCCosto { get; set; }
    public decimal? CBPieza { get; set; }
    public decimal? CNPieza { get; set; }
}

/// <summary>
/// Totales de factura (sum de piezas vinculadas)
/// </summary>
public class FacturaTotalesDto
{
    public decimal Bruto { get; set; }
    public decimal Neto { get; set; }
    public int CantidadPiezas { get; set; }
}

/// <summary>
/// Proveedor para combo searchable
/// </summary>
public class ProveedorComboDto
{
    public int Proveedor { get; set; }
    public string NombreProveedor { get; set; } = string.Empty;
}

/// <summary>
/// Razón social para combo searchable
/// </summary>
public class RazonSocialComboDto
{
    public int IdRazonSocialProveedor { get; set; }
    public string RazonSocialProveedor { get; set; } = string.Empty;
}

/// <summary>
/// Request para asignar pieza individual a factura
/// </summary>
public class AsignarPiezaRequest
{
    public string CodigoBarras { get; set; } = string.Empty;
    public int IdFactura { get; set; }
    public decimal CBTotal { get; set; }
    public decimal CNTotal { get; set; }
    public decimal TCCosto { get; set; }
}

/// <summary>
/// Request para crear/editar factura
/// </summary>
public class FacturaFormRequest
{
    public string FolioFactura { get; set; } = string.Empty;
    public DateTime FechaFactura { get; set; }
    public int Proveedor { get; set; }
    public int IdRazonSocialProveedor { get; set; }
    public string? Pedimento { get; set; }
}
