namespace DiamondsWeb.Models;

/// <summary>
/// Período de inventario físico (tabla InventariosFisicos).
/// Migrado de frmRegistroPeriodos.frm (VB6).
/// </summary>
public class PeriodoInventarioDetalle
{
    public int IdPeriodo { get; set; }
    public DateTime? PeriodoDesde { get; set; }
    public DateTime? PeriodoHasta { get; set; }
    public DateTime? FechaCaptura { get; set; }
    public DateTime? FechaUltEdicion { get; set; }
    public int? IdUsuario { get; set; }
    public string? NombreUsuario { get; set; }
}
