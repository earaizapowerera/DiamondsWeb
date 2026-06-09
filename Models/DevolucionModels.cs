namespace DiamondsWeb.Models;

/// <summary>
/// Registro de devolucion a proveedor (tabla devoluciones)
/// </summary>
public class DevolucionItem
{
    public string CodigoBarras { get; set; } = string.Empty;
    public string? MotivoDevolucion { get; set; }
    public string? Descripcion { get; set; }
    public decimal? Peso { get; set; }
    public decimal? CBTotal { get; set; }
    public decimal? CNTotal { get; set; }
    public string? Remision { get; set; }
    public DateTime FechaDevolucion { get; set; }
    public int? IdUsuario { get; set; }
    public int? Proveedor { get; set; }
    public string? NombreProveedor { get; set; }
}

/// <summary>
/// Info de pieza para validar antes de registrar devolucion
/// </summary>
public class PiezaInfo
{
    public string CodigoBarras { get; set; } = string.Empty;
    public DateTime FechaCaptura { get; set; }
    public string? Descripcion { get; set; }
    public int Precio { get; set; }
}

/// <summary>
/// Estadisticas del dashboard de devoluciones
/// </summary>
public class DevolucionStats
{
    public int TotalDevoluciones { get; set; }
    public int PendientesRemision { get; set; }
    public int ConRemision { get; set; }
    public int DevolucionesHoy { get; set; }
}
