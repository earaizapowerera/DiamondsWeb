namespace DiamondsWeb.Models;

/// <summary>
/// DTO para la tabla InventariosFisicos (períodos de inventario físico).
/// Origen VB6: frmRegistroPeriodos.frm → tabla InventariosFisicos.
/// </summary>
public class PeriodoItem
{
    public int IdPeriodo { get; set; }
    public DateTime? PeriodoDesde { get; set; }
    public DateTime? PeriodoHasta { get; set; }
    public DateTime? FechaCaptura { get; set; }
    public DateTime? FechaUltEdicion { get; set; }
    public int? IdUsuario { get; set; }

    /// <summary>Nombre del usuario (JOIN con tabla USUARIOS).</summary>
    public string? NombreUsuario { get; set; }
}
