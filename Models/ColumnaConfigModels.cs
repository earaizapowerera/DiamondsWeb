namespace DiamondsWeb.Models;

/// <summary>
/// Configuración guardada de columnas (tabla TablasColumnas).
/// </summary>
public class TablaColumnas
{
    public int IdTablaColumnas { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string Vista { get; set; } = string.Empty;
    public DateTime? FechaCaptura { get; set; }
    public DateTime? FechaUltEdicion { get; set; }
    public int? UsuarioId { get; set; }
}

/// <summary>
/// Detalle de columna dentro de una configuración (tabla Columnas).
/// Ancho = 0 significa columna oculta.
/// </summary>
public class ColumnaDetalle
{
    public int IdTablaColumnas { get; set; }
    public string Columna { get; set; } = string.Empty;
    public int Ancho { get; set; }
}

/// <summary>
/// DTO para crear una nueva configuración de columnas.
/// </summary>
public class CrearColumnaConfigRequest
{
    public string Descripcion { get; set; } = string.Empty;
    public string Vista { get; set; } = string.Empty;
    public List<ColumnaVisibilidad> Columnas { get; set; } = new();
}

/// <summary>
/// Visibilidad de una columna individual.
/// </summary>
public class ColumnaVisibilidad
{
    public string Columna { get; set; } = string.Empty;
    public bool Visible { get; set; } = true;
}

/// <summary>
/// Respuesta con configuración completa (header + columnas).
/// </summary>
public class ColumnaConfigResponse
{
    public int IdTablaColumnas { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string Vista { get; set; } = string.Empty;
    public List<ColumnaVisibilidad> Columnas { get; set; } = new();
}
