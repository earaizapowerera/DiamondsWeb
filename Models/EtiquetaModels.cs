namespace DiamondsWeb.Models;

/// <summary>
/// Diseño de etiqueta para el catálogo de configuración.
/// Tabla: DisenosEtiquetas. Usado por CatalogService.ObtenerDiseniosEtiquetasAsync.
/// </summary>
public class DisenioEtiqueta
{
    public int IdDisenio { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string? ArchivoEtiqueta { get; set; }
    public string? ArchivoEtiquetaCompuesta { get; set; }
}

/// <summary>
/// Plantilla de etiqueta almacenada en la tabla diseñosetiquetas.
/// En VB6 legacy estas eran archivos .btw de BarTender.
/// </summary>
public class DisenoEtiqueta
{
    public int IdDisenoEtiqueta { get; set; }
    public string Archivo { get; set; } = string.Empty;
}

/// <summary>
/// Configuración actual de etiquetas leída de la tabla contador.
/// Almacena qué plantilla de etiqueta sencilla está activa
/// y el nombre de la plantilla compuesta.
/// </summary>
public class ConfiguracionEtiqueta
{
    public int IdDisenoEtiqueta { get; set; }
    public string ArchivoEtiquetaCompuesta { get; set; } = string.Empty;
}
