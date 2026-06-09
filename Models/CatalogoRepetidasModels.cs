namespace DiamondsWeb.Models;

/// <summary>
/// Pieza estándar reutilizable del Catálogo de Repetidas.
/// Tabla: CatalogoRepetidas, Vista: vCatalogoRepetidas
/// </summary>
public class RepetidaItem
{
    public string CodigoBarras { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int Proveedor { get; set; }
    public int IdGrupo { get; set; }
    public short? Kilates { get; set; }
    public int? Precio { get; set; }
    public DateTime FechaCaptura { get; set; }
    public DateTime? FechaUltEdicion { get; set; }
    public int IdUsuario { get; set; }
    public int IdDivisor { get; set; }

    // Campos de la vista (JOINs)
    public string NombreProveedor { get; set; } = string.Empty;
    public string Grupo { get; set; } = string.Empty;
    public decimal Divisor { get; set; }
    public string DescDivisor { get; set; } = string.Empty;
}

/// <summary>
/// DTO para crear o editar una pieza repetida
/// </summary>
public class RepetidaForm
{
    public string? CodigoBarras { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public int Proveedor { get; set; }
    public int IdGrupo { get; set; }
    public short? Kilates { get; set; }
    public int? Precio { get; set; }
    public int IdDivisor { get; set; }
}

/// <summary>
/// Item de catálogo para dropdowns (Proveedores, Grupos, Divisores)
/// </summary>
public class CatalogoDropdownItem
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

/// <summary>
/// Item de Divisor con valor decimal para dropdown
/// </summary>
public class DivisorDropdownItem
{
    public int IdDivisor { get; set; }
    public decimal Divisor { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string DisplayText => $"{Divisor:N4} - {Descripcion}";
}
