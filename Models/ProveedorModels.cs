namespace DiamondsWeb.Models;

/// <summary>
/// Proveedor con datos de la vista vProveedores (incluye joins a catálogos)
/// </summary>
public class ProveedorResumen
{
    public int Proveedor { get; set; }
    public string NombreProveedor { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Telefono2 { get; set; }
    public string? Atiende { get; set; }
    public string CaracteristicaDefault { get; set; } = string.Empty;
    public string CostoDefault { get; set; } = string.Empty;
    public string? Moneda { get; set; }
    public string? DefaultUtilidad { get; set; }
    public bool UtilizarMoneda { get; set; }
    public bool UtilidadExtra { get; set; }
    public DateTime FechaCaptura { get; set; }
}

/// <summary>
/// Detalle completo de un proveedor (para crear/editar)
/// </summary>
public class ProveedorDetalle
{
    public int Proveedor { get; set; }
    public string NombreProveedor { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Telefono2 { get; set; }
    public string? Atiende { get; set; }
    public int IdDefaultUtilidad { get; set; }
    public string? DefaultUtilidad { get; set; }
    public int? IdDefaultUtilidadExtra { get; set; }
    public int? IdMoneda { get; set; }
    public bool UtilidadExtra { get; set; }
    public string CaracteristicaDefault { get; set; } = "Oro";
    public string CostoDefault { get; set; } = "Pieza";
    public int IdDivisor { get; set; }
    public int IdTabla { get; set; }
    public bool UtilizarMoneda { get; set; }

    // Campos de solo lectura del view
    public decimal? DefaultUtilidadOro { get; set; }
    public decimal? DefaultUtilidadGemas { get; set; }
    public decimal? DefaultUtilidadReloj { get; set; }
    public decimal? DefaultUtilidadExtraVal { get; set; }
    public string? Moneda { get; set; }
    public decimal? Divisor { get; set; }
    public string? DivisorDescripcion { get; set; }
    public string? TablaDescripcion { get; set; }
}

/// <summary>
/// Item genérico para dropdowns con Id int y texto
/// </summary>
public class CatalogoItem
{
    public int Id { get; set; }
    public string Texto { get; set; } = string.Empty;
}

/// <summary>
/// Item de DefaultsUtilidad con sus valores de utilidad por tipo
/// </summary>
public class DefaultUtilidadItem
{
    public int IdDefaultUtilidad { get; set; }
    public decimal DefaultUtilidad { get; set; }
    public decimal DefaultUtilidadGemas { get; set; }
    public decimal DefaultUtilidadReloj { get; set; }
}

/// <summary>
/// Item de Divisor con su valor numérico
/// </summary>
public class DivisorItem
{
    public int IdDivisor { get; set; }
    public decimal Divisor { get; set; }
    public string? Descripcion { get; set; }
}
