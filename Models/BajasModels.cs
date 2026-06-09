namespace DiamondsWeb.Models;

/// <summary>
/// Representa una pieza vendida/dada de baja desde la vista vBajasPiezas.
/// Columnas de resumen: CodigoBarras, Descripcion, Modelo, Linea, Precio, Proveedor, Obs2, FechaBaja, NombreCliente.
/// </summary>
public class BajaPiezaItem
{
    // — Columnas de resumen (visibles siempre) —
    public string CodigoBarras { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Modelo { get; set; }
    public string? Linea { get; set; }
    public int Precio { get; set; }
    public string? NombreCliente { get; set; }
    public DateTime? FechaBaja { get; set; }
    public string? Obs2 { get; set; }

    // — Columnas de detalle (solo visibles en modo "Todas las Columnas") —
    public int? IdNota { get; set; }
    public decimal Peso { get; set; }
    public decimal PrecioGramo { get; set; }
    public string? Kilates { get; set; }
    public decimal Quilates { get; set; }
    public string? Color { get; set; }
    public string? Pureza { get; set; }
    public string? Corte { get; set; }
    public string? NumSerie { get; set; }
    public string? Obs1 { get; set; }
    public string? Grupo { get; set; }
    public string? Moneda { get; set; }
    public DateTime? FechaCaptura { get; set; }
}

/// <summary>
/// Estadísticas agregadas de la consulta actual.
/// </summary>
public class BajasStats
{
    public int TotalPiezas { get; set; }
    public decimal SumaPrecio { get; set; }
}
