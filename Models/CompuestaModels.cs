namespace DiamondsWeb.Models;

/// <summary>
/// Pieza compuesta para la pagina de administracion de piezas compuestas
/// </summary>
public class PiezaCompuesta
{
    public string CodigoBarras { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string? EtiquetaK { get; set; }
    public string? Linea1 { get; set; }
    public string? Linea2 { get; set; }
    public string? Linea3 { get; set; }
    public int? Componentes { get; set; }
    public int IdUsuario { get; set; }
    public decimal? Precio { get; set; }
    public decimal? PrecioTotal { get; set; }
    public DateTime? FechaCaptura { get; set; }
    public DateTime? FechaUltEdicion { get; set; }
}

/// <summary>
/// Componente de una pieza compuesta
/// </summary>
public class ComponenteCompuesta
{
    public string CodigoBarras { get; set; } = string.Empty;
    public string CBPadre { get; set; } = string.Empty;
    public int Indice { get; set; }
    public string? Descripcion { get; set; }
    public decimal? Precio { get; set; }
    public string? NombreProveedor { get; set; }
}

/// <summary>
/// Resumen de pieza compuesta para listado principal
/// </summary>
public class CompuestaResumen
{
    public string CodigoBarras { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public int IdGrupo { get; set; }
    public string Grupo { get; set; } = "";
    public int Componentes { get; set; }
    public decimal PrecioTotal { get; set; }
    public DateTime FechaCaptura { get; set; }
    public DateTime FechaUltEdicion { get; set; }
}

/// <summary>
/// Detalle completo de pieza compuesta con sus componentes
/// </summary>
public class CompuestaDetalle
{
    public string CodigoBarras { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public int IdGrupo { get; set; }
    public int EtiquetaK { get; set; }
    public int Linea1 { get; set; }
    public int Linea2 { get; set; }
    public int Linea3 { get; set; }
    public int Componentes { get; set; }
    public int IdLocalizacion { get; set; }
    public DateTime FechaCaptura { get; set; }
    public DateTime FechaUltEdicion { get; set; }
    public int IdUsuario { get; set; }
    public List<ComponenteDetalle> ListaComponentes { get; set; } = new();
    public decimal PrecioTotal => ListaComponentes.Sum(c => c.Precio);
}

/// <summary>
/// Detalle de un componente (pieza individual) dentro de una compuesta
/// </summary>
public class ComponenteDetalle
{
    public string CodigoBarras { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string? Kilates { get; set; }
    public string? Modelo { get; set; }
    public string? Linea { get; set; }
    public decimal Quilates { get; set; }
    public string? Color { get; set; }
    public string? Pureza { get; set; }
    public string? Corte { get; set; }
    public string? Obs1 { get; set; }
    public string? Obs2 { get; set; }
    public int Precio { get; set; }
    public int? Proveedor { get; set; }
    public string? NumSerie { get; set; }
    public int Indice { get; set; }
}

/// <summary>
/// Grupo de catálogo para dropdown
/// </summary>
public class GrupoCatalogo
{
    public int IdGrupo { get; set; }
    public string Grupo { get; set; } = "";
}

/// <summary>
/// Request para crear/actualizar una compuesta
/// </summary>
public class CompuestaRequest
{
    public string? CodigoBarras { get; set; }
    public string Descripcion { get; set; } = "";
    public int IdGrupo { get; set; }
    public int EtiquetaK { get; set; }
    public int Linea1 { get; set; }
    public int Linea2 { get; set; }
    public int Linea3 { get; set; }
    public List<string> ComponentesCB { get; set; } = new();
}
