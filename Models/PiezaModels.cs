namespace DiamondsWeb.Models;

// ========== DTOs para Alta de Piezas Sencillas ==========

/// <summary>
/// Pieza individual de joyeria con todos sus costos y caracteristicas.
/// Mapea 1:1 a la tabla PIEZAS.
/// </summary>
public class Pieza
{
    public string CodigoBarras { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public int? IdRemision { get; set; }
    public int? IdFactura { get; set; }
    public int IdGrupo { get; set; }

    // Costos Por Pieza
    public decimal? CBPieza { get; set; }
    public decimal? DescPieza { get; set; }
    public decimal? CNPieza { get; set; }

    // Costos Por Peso
    public decimal? Peso { get; set; }
    public decimal? PrecioGramo { get; set; }
    public decimal? CBPeso { get; set; }
    public decimal? DescPeso { get; set; }
    public decimal? CNPeso { get; set; }

    // Costos Mano de Obra (Extras)
    public decimal? CBManoObra { get; set; }
    public decimal? DescManoObra { get; set; }
    public decimal? CNManoObra { get; set; }
    public string? DescripcionManoObra { get; set; }

    // Totales
    public decimal? CBTotal { get; set; }
    public decimal? CNTotal { get; set; }

    // Costos Factura
    public decimal? CBFactura { get; set; }
    public decimal? DescFactura { get; set; }
    public decimal? CNFactura { get; set; }

    // Factores de precio
    public int IdMoneda { get; set; } = 1;
    public decimal? TCCotizacion { get; set; } = 1;
    public decimal? TCCosto { get; set; }
    public decimal? Utilidad { get; set; } = 1;
    public decimal? UtilidadExtra { get; set; } = 1;
    public decimal? Impuesto { get; set; } = 1;
    public decimal? Divisor { get; set; } = 1;
    public int? Precio { get; set; }

    // Caracteristicas Oro
    public string? Kilates { get; set; }
    public string? Modelo { get; set; }
    public string? Linea { get; set; }

    // Caracteristicas Diamante
    public decimal Quilates { get; set; }
    public string? Color { get; set; }
    public string? Pureza { get; set; }
    public string? Corte { get; set; }

    // Caracteristicas Reloj
    public string? NumSerie { get; set; }
    public string? Obs1 { get; set; }
    public string? Obs2 { get; set; }

    // Metadata
    public DateTime? FechaCaptura { get; set; }
    public int IdUsuario { get; set; }
    public DateTime? FechaUltEdicion { get; set; }
    public int? IdDivisor { get; set; }
    public int? IdTienda { get; set; }
    public int? IdLocalizacion { get; set; }
    public string? ArchivoFoto { get; set; }
    public bool Faltante { get; set; }
    public int? IdStatus { get; set; }
    public string? CBPadre { get; set; }

    // Campos adicionales
    public string? Corte2 { get; set; }
    public bool PrecioEnPesos { get; set; }
    public string? Origen { get; set; }
    public string? Registro { get; set; }

    // Campos calculados/join (no en tabla)
    public string? NombreProveedor { get; set; }
    public string? NumeroRemision { get; set; }
    public string? NombreGrupo { get; set; }
    public string? NombreMoneda { get; set; }
    public string? Observaciones { get; set; }

    // Propiedades de presentacion/join adicionales
    public int? Proveedor { get; set; }
    public string? Grupo { get; set; }
    public string? StatusNombre => IdStatus switch
    {
        1 => "Activa",
        2 => "Vendida",
        3 => "Baja",
        _ => null
    };
}

/// <summary>
/// Proveedor con sus defaults para el formulario
/// </summary>
public class ProveedorInfo
{
    public int Proveedor { get; set; }
    public string NombreProveedor { get; set; } = "";
    public string? DefaultUtilidad { get; set; }
    public int? IdDefaultUtilidadExtra { get; set; }
    public int? IdMoneda { get; set; }
    public bool UtilidadExtra { get; set; }
    public string CaracteristicaDefault { get; set; } = "Oro";
    public string CostoDefault { get; set; } = "Pieza";
    public int IdDivisor { get; set; }
    public int IdTabla { get; set; }
    public bool UtilizarMoneda { get; set; }
}

/// <summary>
/// Divisor de venta
/// </summary>
public class DivisorVenta
{
    public int IdDivisor { get; set; }
    public decimal Divisor { get; set; }
    public string? Descripcion { get; set; }
}

/// <summary>
/// Grupo/categoria de pieza
/// </summary>
public class GrupoPieza
{
    public int IdGrupo { get; set; }
    public string Grupo { get; set; } = "";
}

/// <summary>
/// Etiqueta/plantilla de impresion
/// </summary>
public class EtiquetaPlantilla
{
    public int IdTabla { get; set; }
    public string Descripcion { get; set; } = "";
}

/// <summary>
/// Razon social del proveedor
/// </summary>
public class RazonSocialProveedorCombo
{
    public int IdRazonSocialProveedor { get; set; }
    public string RazonSocial { get; set; } = "";
}

/// <summary>
/// Rango de utilidad extra por precio gramo
/// </summary>
public class UtilidadExtraRango
{
    public int Id { get; set; }
    public decimal PrecioGramoDesde { get; set; }
    public decimal PrecioGramoHasta { get; set; }
    public decimal DefaultUtilidadExtra { get; set; }
}

/// <summary>
/// Resumen de piezas por remision (para la grilla)
/// </summary>
public class PiezaResumen
{
    public string CodigoBarras { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string? NombreGrupo { get; set; }
    public decimal CBTotal { get; set; }
    public decimal CNTotal { get; set; }
    public int Precio { get; set; }
    public decimal Peso { get; set; }
    public string? Kilates { get; set; }
    public string? Modelo { get; set; }
    public string? Linea { get; set; }
    public string? NombreMoneda { get; set; }
    public DateTime FechaCaptura { get; set; }
}

/// <summary>
/// Resultado de guardar pieza
/// </summary>
public class GuardarPiezaResult
{
    public bool Success { get; set; }
    public string? CodigoBarras { get; set; }
    public string? Error { get; set; }
}
