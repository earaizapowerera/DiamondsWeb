namespace DiamondsWeb.Models;

/// <summary>
/// Registro de inventario físico (tabla InventarioFisico)
/// </summary>
public class InventarioFisicoItem
{
    public int Id { get; set; }
    public string CodigoBarras { get; set; } = string.Empty;
    public DateTime FechaCaptura { get; set; }
    public DateTime? FechaUltEdicion { get; set; }
    public int IdUsuario { get; set; }

    // Campos de join con piezas/vCompuestas
    public string? Descripcion { get; set; }
    public decimal? Precio { get; set; }
    public string? Origen { get; set; } // "Pieza", "Compuesta", "Sobrante"
}

/// <summary>
/// Registro de sobrante (pieza no encontrada en catálogo)
/// </summary>
public class SobranteItem
{
    public string CodigoBarras { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal? Precio { get; set; }
    public DateTime FechaCaptura { get; set; }
    public DateTime FechaUltEdicion { get; set; }
    public int IdUsuario { get; set; }
}

/// <summary>
/// Info básica de una pieza (para mostrar al escanear)
/// </summary>
public class PiezaInfo
{
    public string CodigoBarras { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal? Precio { get; set; }
    public bool EsCompuesta { get; set; }
    public List<string> ComponentesCB { get; set; } = new();
}

/// <summary>
/// Resultado de registrar una existencia
/// </summary>
public class RegistroResultado
{
    public bool Exito { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public string? Descripcion { get; set; }
    public decimal? Precio { get; set; }
    public string Tipo { get; set; } = string.Empty; // "Pieza", "Compuesta", "Sobrante"
    public bool RequiereDescripcion { get; set; }
}

/// <summary>
/// Registro cancelado (tabla inventariofisicocancelado)
/// </summary>
public class InventarioCancelado
{
    public int Id { get; set; }
    public string CodigoBarras { get; set; } = string.Empty;
    public DateTime FechaCaptura { get; set; }
    public DateTime? FechaUltEdicion { get; set; }
    public int IdUsuario { get; set; }
    public DateTime FechaCancelacion { get; set; }
    public int CanceladoPor { get; set; }
}

/// <summary>
/// Estadísticas del inventario para dashboard
/// </summary>
public class InventarioStats
{
    public int TotalRegistrosHoy { get; set; }
    public int TotalRegistros { get; set; }
    public int TotalSobrantes { get; set; }
    public int TotalCancelados { get; set; }
}
