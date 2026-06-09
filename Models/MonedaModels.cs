namespace DiamondsWeb.Models;

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
