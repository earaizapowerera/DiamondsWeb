namespace DiamondsWeb.Models;

// ── Pre Bajas ──
public class PreBaja
{
    public string CodigoBarras { get; set; } = "";
    public int IdTipoBaja { get; set; }
    public string? TipoBaja => IdTipoBaja == 1 ? "Venta" : "Devolución";
    public string? Descripcion { get; set; }
    public DateTime? FechaCaptura { get; set; }
}
