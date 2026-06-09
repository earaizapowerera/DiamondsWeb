namespace DiamondsWeb.Models;

public class DefaultUtilidad
{
    public int IdDefaultUtilidad { get; set; }
    public decimal DefaultUtilidadGeneral { get; set; }
    public decimal DefaultUtilidadGemas { get; set; }
    public decimal DefaultUtilidadReloj { get; set; }
    public int IdUsuario { get; set; }
    public string NombreUsuario { get; set; } = "";
    public DateTime FechaCaptura { get; set; }
}

/// <summary>
/// Default de utilidad extra — tabla DefaultsUtilidadExtra, vista vDefaultsUtilidadExtra.
/// Migración de frmDefaultsUtilidadExtra.frm (VB6).
/// </summary>
public class DefaultUtilidadExtra
{
    public int IdDefaultUtilidadExtra { get; set; }
    /// <summary>Factor de utilidad extra (ej: 1.050, 1.100, 1.200)</summary>
    public decimal DefaultUtilidadExtra1 { get; set; }
    public int IdUsuario { get; set; }
    public string NombreUsuario { get; set; } = "";
    public DateTime FechaCaptura { get; set; }
}

/// <summary>
/// Default utilidad item para dropdown en proveedores.
/// Same shape as DefaultUtilidad but named for Proveedores/Editar usage.
/// </summary>
public class DefaultUtilidadItem
{
    public int IdDefaultUtilidad { get; set; }
    public decimal DefaultUtilidad { get; set; }
    public decimal DefaultUtilidadGemas { get; set; }
    public decimal DefaultUtilidadReloj { get; set; }
}

/// <summary>
/// Item generico de catalogo (Id + Texto) para dropdowns.
/// Usado en Proveedores/Editar para Monedas y TablasJerarquias.
/// </summary>
public class CatalogoItem
{
    public int Id { get; set; }
    public string Texto { get; set; } = string.Empty;
}

/// <summary>
/// Proveedor detalle para edicion (Proveedores/Editar).
/// </summary>
public class ProveedorDetalle
{
    public int Proveedor { get; set; }
    public string NombreProveedor { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Telefono2 { get; set; }
    public string? Atiende { get; set; }
    public string CaracteristicaDefault { get; set; } = "Oro";
    public string CostoDefault { get; set; } = "Pieza";
    public int? IdDefaultUtilidad { get; set; }
    public int? IdDefaultUtilidadExtra { get; set; }
    public int? IdMoneda { get; set; }
    public bool UtilizarMoneda { get; set; }
    public bool UtilidadExtra { get; set; }
    public int IdDivisor { get; set; }
    public int IdTabla { get; set; }
    // Read-only display fields
    public string? DefaultUtilidad { get; set; }
    public string? Moneda { get; set; }
    public string? DivisorDescripcion { get; set; }
    public string? TablaDescripcion { get; set; }
}

/// <summary>
/// Proveedor resumen para listado (Proveedores/Index).
/// </summary>
public class ProveedorResumen
{
    public int Proveedor { get; set; }
    public string NombreProveedor { get; set; } = string.Empty;
    public string? Atiende { get; set; }
    public string? Telefono { get; set; }
    public string? Telefono2 { get; set; }
    public string CaracteristicaDefault { get; set; } = "Oro";
    public string CostoDefault { get; set; } = "Pieza";
    public bool UtilizarMoneda { get; set; }
    public string? Moneda { get; set; }
    public string? DefaultUtilidad { get; set; }
}

/// <summary>
/// Default factor comun (impuesto + divisor) — tabla defaultsfactorcomunes.
/// Usado por CatalogService y DefaultsFactorComunes page.
/// </summary>
public class DefaultFactorComun
{
    public int IdDefault { get; set; }
    public decimal DefaultImpuesto { get; set; }
    public decimal DefaultDivisor { get; set; }
    public int IdUsuario { get; set; }
    public DateTime? FechaCaptura { get; set; }
}

/// <summary>
/// Divisor para catalogo CRUD (Catalogos/Divisores).
/// </summary>
public class Divisor
{
    public int IdDivisor { get; set; }
    public decimal ValorDivisor { get; set; }
    public string Descripcion { get; set; } = string.Empty;
}

/// <summary>
/// Proveedor para catalogo CRUD (Catalogos/Proveedores y Repetidas).
/// </summary>
public class Proveedor
{
    public int Proveedor1 { get; set; }
    public string? NombreProveedor { get; set; }
    public string? Telefono { get; set; }
    public string? Contacto { get; set; }
    public string? Direccion { get; set; }
    public int? IdDefaultCaracteristica { get; set; }
    public int? IdDefaultTipoCosto { get; set; }
    public int? IdDefaultUtilidad { get; set; }
    public int? IdMoneda { get; set; }
    public bool MonedaDefault { get; set; }
    public bool UtilidadExtraPrecioGramo { get; set; }
    public int? IdUsuario { get; set; }
    public DateTime? FechaCaptura { get; set; }
}

/// <summary>
/// Grupo de pieza para catalogo (Catalogos/Repetidas).
/// </summary>
public class Grupo
{
    public int IdGrupo { get; set; }
    public string Grupo1 { get; set; } = string.Empty;
}
