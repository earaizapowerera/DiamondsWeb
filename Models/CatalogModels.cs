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

/// <summary>
/// Default de utilidad extra — tabla DefaultsUtilidadExtra, vista vDefaultsUtilidadExtra.
/// Migración de frmDefaultsUtilidadExtra.frm (VB6).
/// </summary>
public class DefaultUtilidadExtra
{
    public int IdDefaultUtilidadExtra { get; set; }
    /// <summary>Factor de utilidad extra (ej: 1.050, 1.100, 1.200)</summary>
    public decimal DefaultUtilidadExtra1 { get; set; }
    public int IdUsuario { get; set; }
    public string NombreUsuario { get; set; } = "";
    public DateTime FechaCaptura { get; set; }
}
