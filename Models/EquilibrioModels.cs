namespace DiamondsWeb.Models;

/// <summary>
/// Resultado del cálculo de equilibrio de comisiones.
/// Compara ventas comisionables vs comisiones pagadas en un período.
/// Origen VB6: frmEquilibrio.frm (RecursosHumanos.vbp).
/// </summary>
public class EquilibrioResultado
{
    /// <summary>Total de ventas comisionables (piezasnotas con umbral por grupo/proveedor).</summary>
    public decimal VentasComisionables { get; set; }

    /// <summary>Total de comisiones pagadas (tabla mr).</summary>
    public decimal ComisionesPagadas { get; set; }

    /// <summary>Balance = VentasComisionables - ComisionesPagadas.</summary>
    public decimal Balance => VentasComisionables - ComisionesPagadas;

    /// <summary>True si el balance es positivo (hay ventas pendientes de comisionar).</summary>
    public bool EsPositivo => Balance > 0;
}

/// <summary>
/// Tienda para el dropdown de filtro.
/// </summary>
public class TiendaItem
{
    public int IdTienda { get; set; }
    public string NombreTienda { get; set; } = "";
}
