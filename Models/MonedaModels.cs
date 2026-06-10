namespace DiamondsWeb.Models;

/// <summary>
/// Moneda base — origen: tabla Monedas
/// </summary>
public class Moneda
{
    public int IdMoneda { get; set; }
    public string NombreMoneda { get; set; } = "";
    public bool Extranjera { get; set; }

    /// <summary>Alias for NombreMoneda — used by Razor views expecting Moneda1</summary>
    public string Moneda1 => NombreMoneda;
}

/// <summary>
/// Tipo de cambio por moneda — origen: tabla tiposcambio / vista vTiposCambio
/// </summary>
public class TipoCambio
{
    public int IdTipoCambio { get; set; }
    public int IdMoneda { get; set; }
    public decimal TipoCambioCotizacion { get; set; }
    public decimal TipoCambioVenta { get; set; }

    // Extended fields from JOIN (used by TiposCambio catalog page)
    public string? Moneda { get; set; }
    public int? IdUsuario { get; set; }
    public DateTime? FechaCaptura { get; set; }
}

/// <summary>
/// Modelo de moneda con nombre de usuario (vista vMonedas).
/// Separado de CatalogModels para evitar colisión con Moneda legacy.
/// </summary>
public class MonedaDetalle
{
    public int IdMoneda { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Extranjera { get; set; }
    public int IdUsuario { get; set; }
    public string? NombreUsuario { get; set; }
    public DateTime FechaCaptura { get; set; }
}
