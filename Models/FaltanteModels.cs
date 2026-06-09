namespace DiamondsWeb.Models;

/// <summary>
/// Pieza marcada como faltante en inventario fisico.
/// Origen: PIEZAS WHERE Faltante=1 + ComentariosFaltantes + StatusPiezas.
/// </summary>
public class PiezaFaltante
{
    public string CodigoBarras { get; set; } = "";
    public string? Descripcion { get; set; }
    public string? Modelo { get; set; }
    public string? Linea { get; set; }
    public string? Kilates { get; set; }
    public decimal Peso { get; set; }
    public decimal CBTotal { get; set; }
    public int Precio { get; set; }
    public string? Status { get; set; }
    public string? Comentarios { get; set; }
}

/// <summary>
/// Estadisticas resumen para el dashboard de faltantes.
/// </summary>
public class FaltantesStats
{
    public int TotalFaltantes { get; set; }
    public int ConComentarios { get; set; }
    public int SinComentarios { get; set; }
    public decimal ValorTotal { get; set; }
}
