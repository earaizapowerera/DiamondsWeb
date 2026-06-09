namespace DiamondsWeb.Models;

public class DefaultUtilidad
{
    public int IdDefaultUtilidad { get; set; }
    public decimal DefaultUtilidadGeneral { get; set; }
    public decimal DefaultUtilidadGemas { get; set; }
    public decimal DefaultUtilidadReloj { get; set; }
    public int IdUsuario { get; set; }
    public string NombreUsuario { get; set; } = "";
    public DateTime FechaCaptura { get; set; }
}
