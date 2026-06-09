namespace DiamondsWeb.Models;

/// <summary>
/// Tipo de cambio registrado (vista vTiposCambio)
/// </summary>
public class TipoCambioItem
{
    public int IdTipoCambio { get; set; }
    public decimal TipoCambioCotizacion { get; set; }
    public decimal? TipoCambioVenta { get; set; }
    public int IdMoneda { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public int IdUsuario { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public DateTime FechaCaptura { get; set; }
}

/// <summary>
/// Moneda del catalogo
/// </summary>
public class MonedaItem
{
    public int IdMoneda { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public bool Extranjera { get; set; }
}

/// <summary>
/// Ultimo tipo de cambio vigente por moneda (resumen)
/// </summary>
public class TipoCambioVigente
{
    public int IdMoneda { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public decimal TipoCambioCotizacion { get; set; }
    public decimal? TipoCambioVenta { get; set; }
    public DateTime FechaCaptura { get; set; }
}
