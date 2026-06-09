namespace DiamondsWeb.Models;

// ── Piezas Faltantes ──
public class PiezaFaltante
{
    public string CodigoBarras { get; set; } = "";
    public string? Descripcion { get; set; }
    public decimal? Precio { get; set; }
    public string? Grupo { get; set; }
    public string? Modelo { get; set; }
    public string? Linea { get; set; }
    public string? Kilates { get; set; }
    public decimal? Peso { get; set; }
    public string? NumSerie { get; set; }
    public string? Comentario { get; set; }
}

// ── Pre Bajas ──
public class PreBaja
{
    public string CodigoBarras { get; set; } = "";
    public int IdTipoBaja { get; set; }
    public string? TipoBaja => IdTipoBaja == 1 ? "Venta" : "Devolución";
    public string? Descripcion { get; set; }
    public DateTime? FechaCaptura { get; set; }
}
