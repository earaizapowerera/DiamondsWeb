namespace DiamondsWeb.Models;

/// <summary>
/// Representa un diamante desde la vista vDiamantes.
/// Columnas: IdLocalizacion, NombreStatus, Corte, Corte2, Quilates,
///           Color, Pureza, Obs2, Precio, Descripcion, Obs1, CodigoBarras,
///           Proveedor, IdTienda, CBPadre, Grupo
/// </summary>
public class Diamante
{
    public int IdLocalizacion { get; set; }
    public string NombreStatus { get; set; } = string.Empty;
    public string Corte { get; set; } = string.Empty;
    public string? Corte2 { get; set; }
    public decimal Quilates { get; set; }
    public string Color { get; set; } = string.Empty;
    public string Pureza { get; set; } = string.Empty;
    public string? Obs2 { get; set; }
    public int Precio { get; set; }
    public string? Descripcion { get; set; }
    public string? Obs1 { get; set; }
    public string? CodigoBarras { get; set; }
    public int Proveedor { get; set; }
    public int IdTienda { get; set; }
    public string? CBPadre { get; set; }
    public string? Grupo { get; set; }
}

/// <summary>
/// Diamante para listado en catálogo (vista vdiamantes, subset de columnas).
/// Usado por CatalogService.ObtenerDiamantesAsync.
/// </summary>
public class DiamanteLista
{
    public string CodigoBarras { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal? Quilates { get; set; }
    public string? Color { get; set; }
    public string? Pureza { get; set; }
    public string? Corte { get; set; }
    public string? Obs1 { get; set; }
    public string? Obs2 { get; set; }
    public int? Precio { get; set; }
    public string? NombreProveedor { get; set; }
}

/// <summary>
/// Filtros para la búsqueda de diamantes
/// </summary>
public class DiamanteFiltros
{
    public string? Busqueda { get; set; }
    public string? Corte { get; set; }
    public string? Color { get; set; }
    public string? Pureza { get; set; }
    public string? Status { get; set; }
    public decimal? QuilatesMin { get; set; }
    public decimal? QuilatesMax { get; set; }
    public int? PrecioMin { get; set; }
    public int? PrecioMax { get; set; }
}
