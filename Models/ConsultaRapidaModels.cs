namespace DiamondsWeb.Models;

/// <summary>
/// Pieza en existencia (vista vpiezas).
/// </summary>
public class PiezaExistencia
{
    public string CodigoBarras { get; set; } = "";
    public string? Descripcion { get; set; }
    public string? Grupo { get; set; }
    public string? Modelo { get; set; }
    public string? Linea { get; set; }
    public string? Kilates { get; set; }
    public decimal? Quilates { get; set; }
    public string? Color { get; set; }
    public string? Pureza { get; set; }
    public string? Corte { get; set; }
    public decimal? Peso { get; set; }
    public decimal? CBTotal { get; set; }
    public decimal? CNTotal { get; set; }
    public string? Moneda { get; set; }
    public int? Precio { get; set; }
    public string? NumSerie { get; set; }
    public string? Obs1 { get; set; }
    public string? Obs2 { get; set; }
    public string? Remision { get; set; }
    public int? Proveedor { get; set; }
    public DateTime? FechaCaptura { get; set; }
}

/// <summary>
/// Pieza vendida o devuelta (vista vbajaspiezas).
/// </summary>
public class PiezaVendida
{
    public string CodigoBarras { get; set; } = "";
    public int? IdNota { get; set; }
    public string? NombreCliente { get; set; }
    public string? Descripcion { get; set; }
    public string? Modelo { get; set; }
    public string? Linea { get; set; }
    public decimal? Peso { get; set; }
    public decimal? CBTotal { get; set; }
    public decimal? CNTotal { get; set; }
    public int? Precio { get; set; }
    public string? Kilates { get; set; }
    public decimal? Quilates { get; set; }
    public string? Color { get; set; }
    public string? Pureza { get; set; }
    public DateTime? FechaCaptura { get; set; }
}

/// <summary>
/// Pieza cancelada (tabla piezascanceladas).
/// </summary>
public class PiezaCancelada
{
    public string CodigoBarras { get; set; } = "";
    public string? Descripcion { get; set; }
    public string? Modelo { get; set; }
    public string? Linea { get; set; }
    public decimal? Peso { get; set; }
    public decimal? CBTotal { get; set; }
    public decimal? CNTotal { get; set; }
    public int? Precio { get; set; }
    public string? Kilates { get; set; }
    public int? IdUsuario { get; set; }
    public int? IdStatus { get; set; }
    public DateTime? FechaCaptura { get; set; }
}

/// <summary>
/// Devolucion a proveedor (tabla devoluciones).
/// </summary>
public class DevolucionProveedor
{
    public string CodigoBarras { get; set; } = "";
    public string? MotivoDevolucion { get; set; }
    public string? Remision { get; set; }
    public DateTime? FechaDevolucion { get; set; }
    public int? IdUsuario { get; set; }
}

/// <summary>
/// Resultado consolidado de la consulta rapida por codigo de barras.
/// </summary>
public class ConsultaRapidaResultado
{
    public string CodigoBarras { get; set; } = "";
    public List<PiezaExistencia> Existencias { get; set; } = new();
    public List<PiezaVendida> Vendidas { get; set; } = new();
    public List<PiezaCancelada> Canceladas { get; set; } = new();
    public List<DevolucionProveedor> Devoluciones { get; set; } = new();

    public bool TieneResultados =>
        Existencias.Any() || Vendidas.Any() || Canceladas.Any() || Devoluciones.Any();
}
