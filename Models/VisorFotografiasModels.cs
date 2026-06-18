namespace DiamondsWeb.Models;

/// <summary>
/// Representa una pieza del catálogo fotográfico con su estado de visibilidad (CBO).
/// Fuente: vista vfotografias (piezas LEFT JOIN cbo LEFT JOIN Grupos).
/// </summary>
public class PiezaCbo
{
    public int Visible { get; set; }
    public string CodigoBarras { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string? Grupo { get; set; }
    public string? Kilates { get; set; }
    public string? Modelo { get; set; }
    public int Precio { get; set; }
    public string Cb { get; set; } = "";
}
