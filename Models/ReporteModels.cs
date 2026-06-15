namespace DiamondsWeb.Models;

/// <summary>
/// Pieza con campos extendidos para el reporte de listado.
/// Incluye Peso, CBTotal, CNTotal para cálculo de totales.
/// </summary>
public class PiezaReporte
{
    public string CodigoBarras { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string? Grupo { get; set; }
    public string? NombreProveedor { get; set; }
    public decimal? Peso { get; set; }
    public decimal? CBPieza { get; set; }
    public decimal? CNPieza { get; set; }
    public decimal? CBTotal { get; set; }
    public decimal? CNTotal { get; set; }
    public int? Precio { get; set; }
    public string? Kilates { get; set; }
    public string? Modelo { get; set; }
    public int? IdStatus { get; set; }
    public DateTime? FechaCaptura { get; set; }

    public string StatusNombre => IdStatus switch
    {
        1 => "Activa",
        2 => "Vendida",
        3 => "Baja",
        _ => ""
    };
}

/// <summary>
/// Totales acumulados del listado de piezas (replica la lógica
/// de ImprimirDB del VB6: sum(Peso, CBTotal, CNTotal, CBPieza, CNPieza, Precio)).
/// </summary>
public class TotalesPiezas
{
    public decimal Peso { get; set; }
    public decimal CBPieza { get; set; }
    public decimal CNPieza { get; set; }
    public decimal CBTotal { get; set; }
    public decimal CNTotal { get; set; }
    public decimal Precio { get; set; }
    public int TotalPiezas { get; set; }
}
