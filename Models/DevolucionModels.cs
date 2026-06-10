namespace DiamondsWeb.Models;

/// <summary>
/// Registro de devolucion a proveedor — origen: vista vDevoluciones
/// </summary>
public class DevolucionItem
{
    public string CodigoBarras { get; set; } = string.Empty;
    public string? MotivoDevolucion { get; set; }
    public string? Descripcion { get; set; }
    public decimal? Peso { get; set; }
    public decimal? CBTotal { get; set; }
    public decimal? CNTotal { get; set; }
    public string? Remision { get; set; }
    public DateTime? FechaDevolucion { get; set; }
    public int? IdUsuario { get; set; }
    public int? Proveedor { get; set; }
    public string? NombreProveedor { get; set; }
}

/// <summary>
/// Estadisticas de devoluciones para el dashboard
/// </summary>
public class DevolucionStats
{
    public int TotalDevoluciones { get; set; }
    public int PendientesRemision { get; set; }
    public int ConRemision { get; set; }
    public int DevolucionesHoy { get; set; }
}

/// <summary>
/// Resultado de buscar una pieza vendida por codigo de barras.
/// Contiene info de la compra original para mostrar antes de reestablecer.
/// </summary>
public class PiezaDevolucion
{
    public string CodigoBarras { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public int IdNota { get; set; }
    public decimal Descuento { get; set; }
    public string? NombreCliente { get; set; }
    public DateTime? FechaCompra { get; set; }
    public string? Tienda { get; set; }
    public string? FormaPago { get; set; }

    /// <summary>True si la pieza esta en bajaspiezas (candidata a reestablecer)</summary>
    public bool EnBajas { get; set; }

    /// <summary>Precio pagado = Precio * (1 - Descuento/100)</summary>
    public decimal PrecioPagado => Precio * (1m - (Descuento / 100m));
}

/// <summary>
/// Tienda disponible para seleccionar al reestablecer pieza
/// </summary>
public class TiendaInfo
{
    public int IdTienda { get; set; }
    public string NombreTienda { get; set; } = string.Empty;
}

/// <summary>
/// Resultado de un intento de reestablecimiento
/// </summary>
public class ResultadoReestablecimiento
{
    public bool Exito { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}
