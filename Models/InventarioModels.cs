namespace DiamondsWeb.Models;

/// <summary>
/// Registro de una pieza escaneada durante inventario fisico
/// </summary>
public class RegistroInventario
{
    public int Id { get; set; }
    public string CodigoBarras { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public DateTime FechaCaptura { get; set; }
    public int IdUsuario { get; set; }
    /// <summary>
    /// "Pieza" = encontrada en sistema, "Sobrante" = no encontrada, "Compuesta" = pieza compuesta
    /// </summary>
    public string TipoRegistro { get; set; } = "Pieza";
    /// <summary>
    /// Si fue registrada como componente de una compuesta, el CB padre
    /// </summary>
    public string? CBPadreCompuesta { get; set; }
}

/// <summary>
/// Pieza sobrante (escaneada pero no existe en sistema)
/// </summary>
public class PiezaSobrante
{
    public string CodigoBarras { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int? Precio { get; set; }
    public DateTime FechaCaptura { get; set; }
    public int IdUsuario { get; set; }
}

/// <summary>
/// Estadisticas del inventario fisico actual
/// </summary>
public class InventarioStats
{
    public int TotalEscaneadas { get; set; }
    public int EnSistema { get; set; }
    public int Sobrantes { get; set; }
    public int Compuestas { get; set; }
    public int ComponentesAuto { get; set; }
    public int Faltantes { get; set; }
}

/// <summary>
/// Resultado de escanear un codigo de barras
/// </summary>
public class EscaneoResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string TipoRegistro { get; set; } = string.Empty;
    /// <summary>
    /// True si el codigo no existe en piezas y necesita datos adicionales
    /// </summary>
    public bool RequiereDatosSobrante { get; set; }
    /// <summary>
    /// Si es compuesta, lista de componentes auto-registrados
    /// </summary>
    public List<string> ComponentesRegistrados { get; set; } = new();
    /// <summary>
    /// True si ya fue escaneada anteriormente
    /// </summary>
    public bool YaEscaneada { get; set; }
    public string? Descripcion { get; set; }
    /// <summary>
    /// Stats actualizadas despues del escaneo
    /// </summary>
    public InventarioStats? Stats { get; set; }
}

/// <summary>
/// Pieza faltante (no contada en inventario fisico)
/// </summary>
public class PiezaFaltante
{
    public string CodigoBarras { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int? Precio { get; set; }
}
