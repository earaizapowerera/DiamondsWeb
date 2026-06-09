namespace DiamondsWeb.Models;

/// <summary>
/// Razón social de un proveedor (entidad fiscal)
/// Tabla: RAZONES_SOCIALES_PROVEEDORES
/// </summary>
public class RazonSocialProveedor
{
    public int IdRazonSocialProveedor { get; set; }
    public string? RFC { get; set; }
    public string RazonSocialProveedorNombre { get; set; } = string.Empty;
    public string? Calle { get; set; }
    public string? CodigoPostal { get; set; }
    public string? Colonia { get; set; }
    public string? Municipio { get; set; }
    public string? Estado { get; set; }
    public DateTime? FechaCaptura { get; set; }
    public DateTime? FechaUltEdicion { get; set; }
    public int? IdUsuario { get; set; }
}

/// <summary>
/// Asignación N:N entre razón social y proveedor
/// Tabla: RAZONES_SOCIALES_PROVEEDORES_PROVEEDORES
/// </summary>
public class RazonSocialProveedorAsignacion
{
    public int Id { get; set; }
    public int IdRazonSocialProveedor { get; set; }
    public int Proveedor { get; set; }
    public DateTime FechaCaptura { get; set; }
    public DateTime? FechaUltEdicion { get; set; }
    public int? IdUsuario { get; set; }

    // Campos de la vista vRazonesSocialesProveedoresProveedores
    public string? NombreProveedor { get; set; }
    public string? RazonSocialProveedorNombre { get; set; }
}

/// <summary>
/// Proveedor (catálogo simple para dropdowns)
/// Tabla: PROVEEDORES
/// </summary>
public class ProveedorSimple
{
    public int Proveedor { get; set; }
    public string NombreProveedor { get; set; } = string.Empty;
}
