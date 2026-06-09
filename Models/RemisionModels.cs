namespace DiamondsWeb.Models;

/// <summary>
/// Remision completa con datos de proveedor (vista vBuscaRemisiones)
/// </summary>
public class RemisionResumen
{
    public int IdRemision { get; set; }
    public int Proveedor { get; set; }
    public string NombreProveedor { get; set; } = string.Empty;
    public string Remision { get; set; } = string.Empty;
    public DateTime? FechaRemision { get; set; }
    public bool Consignacion { get; set; }
    public int? IdTienda { get; set; }
    public DateTime FechaCaptura { get; set; }
    public DateTime? FechaUltEdicion { get; set; }
    public int TotalPiezas { get; set; }
    public decimal TotalBruto { get; set; }
    public decimal TotalNeto { get; set; }
}

/// <summary>
/// Pieza disponible para vincular a una remision (vista vActualizaPiezas)
/// </summary>
public class PiezaDisponible
{
    public string CodigoBarras { get; set; } = string.Empty;
    public string? Obs2 { get; set; }
    public int? IdFactura { get; set; }
    public int? IdRemision { get; set; }
    public string? Remision { get; set; }
    public int? Proveedor { get; set; }
    public string? Descripcion { get; set; }
    public DateTime FechaCaptura { get; set; }
    public decimal? TCCosto { get; set; }
    public decimal? CBPieza { get; set; }
    public decimal? CNPieza { get; set; }
    public decimal? DescPieza { get; set; }
    public decimal? CostoMN { get; set; }
    public int IdMoneda { get; set; }
    public decimal? CostoBrutoMN { get; set; }
}

/// <summary>
/// Pieza vinculada a una remision (grid derecho)
/// </summary>
public class PiezaRemision
{
    public string CodigoBarras { get; set; } = string.Empty;
    public string? Obs2 { get; set; }
    public decimal CBTotal { get; set; }
    public decimal CNTotal { get; set; }
    public decimal? TCCosto { get; set; }
    public decimal Bruto { get; set; }
    public decimal Neto { get; set; }
}

/// <summary>
/// Item de dropdown de proveedores
/// </summary>
public class ProveedorItem
{
    public int Proveedor { get; set; }
    public string NombreProveedor { get; set; } = string.Empty;
}

/// <summary>
/// Totales de una remision
/// </summary>
public class RemisionTotales
{
    public decimal Bruto { get; set; }
    public decimal Neto { get; set; }
}
