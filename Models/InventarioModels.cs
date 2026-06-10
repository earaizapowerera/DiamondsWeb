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

    // Propiedades adicionales para el dashboard de inventario fisico
    public int TotalEscaneadas { get; set; }
    public int EnSistema { get; set; }
    public int Sobrantes { get; set; }
    public int Compuestas { get; set; }
    public int ComponentesAuto { get; set; }
    public int Faltantes { get; set; }
}

/// <summary>
/// Registro de inventario fisico escaneado (alias para InventarioFisicoItem con nombre esperado por las paginas)
/// </summary>
public class RegistroInventarioFisico
{
    public int Id { get; set; }
    public string CodigoBarras { get; set; } = string.Empty;
    public DateTime? FechaCaptura { get; set; }
    public DateTime? FechaUltEdicion { get; set; }
    public int IdUsuario { get; set; }
    public string? Descripcion { get; set; }
    public decimal? Precio { get; set; }
    public string? Origen { get; set; }
}

/// <summary>
/// Registro de inventario con informacion de status faltante/sobrante
/// </summary>
public class RegistroInventario
{
    public int Id { get; set; }
    public string CodigoBarras { get; set; } = string.Empty;
    public DateTime FechaCaptura { get; set; }
    public DateTime? FechaUltEdicion { get; set; }
    public int IdUsuario { get; set; }
    public string? Descripcion { get; set; }
    public decimal? Precio { get; set; }
    public string? Origen { get; set; }

    /// <summary>Tipo de registro: Pieza, Compuesta, Componente, Sobrante</summary>
    public string? TipoRegistro { get; set; }

    /// <summary>Codigo de barras del padre si este registro es un componente de compuesta</summary>
    public string? CBPadreCompuesta { get; set; }
}

/// <summary>
/// Pieza sobrante (encontrada en escaneo pero no en catalogo)
/// </summary>
public class PiezaSobrante
{
    public string CodigoBarras { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal? Precio { get; set; }
    public DateTime FechaCaptura { get; set; }
    public DateTime? FechaUltEdicion { get; set; }
    public int IdUsuario { get; set; }
}

/// <summary>
/// Pieza faltante (en catalogo pero no escaneada en inventario)
/// </summary>
public class PiezaFaltante
{
    public string CodigoBarras { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal? Precio { get; set; }
    public string? Grupo { get; set; }
    public string? Comentario { get; set; }
}

/// <summary>
/// Resultado del escaneo de un codigo de barras (respuesta AJAX)
/// </summary>
public class EscaneoResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public string? Descripcion { get; set; }
    public decimal? Precio { get; set; }
    public string? Tipo { get; set; }
    public bool RequiereDescripcion { get; set; }
}
