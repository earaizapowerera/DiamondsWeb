namespace DiamondsWeb.Models;

/// <summary>
/// DTO para la tabla Grupos (catálogo de categorías de productos).
/// Origen VB6: frmGrupos.frm → tabla Grupos + vista vGrupos.
/// </summary>
public class GrupoItem
{
    public int IdGrupo { get; set; }
    public string Grupo { get; set; } = string.Empty;
    public DateTime? FechaCaptura { get; set; }
    public int? IdUsuario { get; set; }
    public DateTime? FechaUltEdicion { get; set; }

    // Viene del JOIN con tabla Usuarios (vista vGrupos)
    public string? Nombre { get; set; }
}
