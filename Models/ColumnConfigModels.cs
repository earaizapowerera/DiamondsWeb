namespace DiamondsWeb.Models;

/// <summary>
/// Configuracion guardada de columnas visibles para una vista.
/// Mapea a tabla TABLASCOLUMNAS.
/// </summary>
public class TablaColumnaConfig
{
    public int IdTablaColumnas { get; set; }
    public string Descripcion { get; set; } = "";
    public string Vista { get; set; } = "";
    public int? UsuarioId { get; set; }
    public DateTime FechaCaptura { get; set; }
    public DateTime FechaUltEdicion { get; set; }
}

/// <summary>
/// Columna individual dentro de una configuracion.
/// Mapea a tabla COLUMNAS.
/// </summary>
public class ColumnaConfig
{
    public int IdTablaColumnas { get; set; }
    public string Columna { get; set; } = "";
    public int Ancho { get; set; }
}

/// <summary>
/// Definicion de una columna disponible en una vista web.
/// No es de BD — se define en codigo como catalogo de columnas por vista.
/// </summary>
public class ColumnDefinition
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public bool DefaultVisible { get; set; } = true;
    public string CssClass { get; set; } = "";
}

/// <summary>
/// Request para guardar la configuracion de columnas del usuario.
/// </summary>
public class GuardarColumnasRequest
{
    public string Vista { get; set; } = "";
    public string Descripcion { get; set; } = "Mi configuracion";
    public List<string> ColumnasVisibles { get; set; } = new();
}

/// <summary>
/// Respuesta con la configuracion de columnas del usuario.
/// </summary>
public class ColumnasUsuarioResponse
{
    public int? IdTablaColumnas { get; set; }
    public string Descripcion { get; set; } = "";
    public List<string> ColumnasVisibles { get; set; } = new();
    public List<ColumnDefinition> TodasLasColumnas { get; set; } = new();
}
