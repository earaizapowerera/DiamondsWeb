namespace DiamondsWeb.Models;

/// <summary>
/// Modelo de un divisor para cálculo de precio de venta.
/// Precio Venta = Costo / Divisor. Multiplicador = 1 / Divisor.
/// Tabla: Divisores
/// </summary>
public class DivisorItem
{
    public int IdDivisor { get; set; }
    public decimal Divisor { get; set; }
    public string? Descripcion { get; set; }
    public int IdUsuario { get; set; }
    public DateTime FechaCaptura { get; set; }
    public DateTime FechaUltEdicion { get; set; }

    /// <summary>
    /// Multiplicador calculado = 1 / Divisor.
    /// Retorna 0 si Divisor es 0 para evitar división por cero.
    /// </summary>
    public decimal Multiplicador => Divisor != 0 ? Math.Round(1m / Divisor, 4) : 0;
}
