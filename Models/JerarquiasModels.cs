namespace DiamondsWeb.Models;

/// <summary>
/// Tabla maestra de jerarquías para etiquetas.
/// Cada tabla define un tipo de etiqueta (Normal, Diamante, Reloj, etc.)
/// </summary>
public class TablaJerarquia
{
    public int IdTablaJerarquia { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public int? IdUsuario { get; set; }
    public DateTime? FechaCaptura { get; set; }

    /// <summary>Cantidad de columnas/jerarquías asociadas (campo calculado)</summary>
    public int CantidadColumnas { get; set; }

    /// <summary>Alias for IdTablaJerarquia — used by some views/pages expecting IdTabla</summary>
    public int IdTabla => IdTablaJerarquia;
}

/// <summary>
/// Columnas/campos de una tabla de jerarquía.
/// Define qué renglones aparecen en la etiqueta.
/// </summary>
public class Jerarquia
{
    public int IdJerarquia { get; set; }
    public int IdTablaJerarquia { get; set; }
    public string? Columna { get; set; }
    public int? Orden { get; set; }
    public DateTime? FechaCaptura { get; set; }
}

/// <summary>
/// Opciones válidas para el campo Columna de Jerarquías.
/// Corresponden a las columnas disponibles en la pieza para la etiqueta.
/// </summary>
public static class ColumnasDisponibles
{
    public static readonly string[] Valores = new[]
    {
        "Diam", "Mod", "Linea", "Obs1", "Obs2", "Ser"
    };

    public static readonly Dictionary<string, string> Etiquetas = new()
    {
        ["Diam"] = "Diamante",
        ["Mod"] = "Modelo",
        ["Linea"] = "Linea",
        ["Obs1"] = "Observacion 1",
        ["Obs2"] = "Observacion 2",
        ["Ser"] = "Serie"
    };
}
