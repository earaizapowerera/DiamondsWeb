namespace DiamondsWeb.Models;

/// <summary>
/// Registro de asistencia (vista vAsistencia: Asistencia JOIN Usuarios).
/// </summary>
public class AsistenciaItem
{
    public int IdAsistencia { get; set; }
    public DateTime FechaCaptura { get; set; }
    public int IdUsuario { get; set; }
    /// <summary>E = Entrada, S = Salida</summary>
    public string Movimiento { get; set; } = string.Empty;
    /// <summary>Nombre del empleado (viene del JOIN con Usuarios).</summary>
    public string Nombre { get; set; } = string.Empty;
}

/// <summary>
/// Empleado disponible para el dropdown de asistencia.
/// </summary>
public class EmpleadoItem
{
    public int IdUsuario { get; set; }
    public string Nombre { get; set; } = string.Empty;
}
