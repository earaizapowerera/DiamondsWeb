namespace DiamondsWeb.Models;

// ── Piezas (tabla principal) ──
public class Pieza
{
    public string CodigoBarras { get; set; } = "";
    public string? Descripcion { get; set; }
    public decimal? Precio { get; set; }
    public int? Proveedor { get; set; }
    public string? NombreProveedor { get; set; }
    public int? IdGrupo { get; set; }
    public string? Grupo { get; set; }
    public string? Kilates { get; set; }
    public string? Modelo { get; set; }
    public string? Linea { get; set; }
    public decimal? Quilates { get; set; }
    public string? Color { get; set; }
    public string? Pureza { get; set; }
    public string? Corte { get; set; }
    public string? NumSerie { get; set; }
    public string? Obs1 { get; set; }
    public string? Obs2 { get; set; }
    public decimal? Peso { get; set; }
    public decimal? PrecioGramo { get; set; }
    public decimal? CBPieza { get; set; }
    public decimal? CNPieza { get; set; }
    public decimal? DescPieza { get; set; }
    public decimal? CBPeso { get; set; }
    public decimal? CNPeso { get; set; }
    public decimal? DescPeso { get; set; }
    public decimal? CBManoObra { get; set; }
    public decimal? CNManoObra { get; set; }
    public decimal? DescManoObra { get; set; }
    public decimal? CBFactura { get; set; }
    public decimal? CNFactura { get; set; }
    public decimal? DescFactura { get; set; }
    public decimal? Utilidad { get; set; }
    public decimal? UtilidadExtra { get; set; }
    public decimal? Impuesto { get; set; }
    public int? IdDivisor { get; set; }
    public int? IdMoneda { get; set; }
    public decimal? TCCosto { get; set; }
    public decimal? TCCotizacion { get; set; }
    public int? IdRemision { get; set; }
    public int? IdFactura { get; set; }
    public int? IdLocalizacion { get; set; }
    public int? IdTienda { get; set; }
    public int? IdUsuario { get; set; }
    public int? IdStatus { get; set; }
    public string? StatusNombre { get; set; }
    public string? CBPadre { get; set; }
    public bool Faltante { get; set; }
    public DateTime? FechaCaptura { get; set; }
    public DateTime? FechaUltEdicion { get; set; }
}

// ── Diamantes (vista vdiamantes) ──
public class DiamanteLista
{
    public string CodigoBarras { get; set; } = "";
    public string? Descripcion { get; set; }
    public decimal? Quilates { get; set; }
    public string? Color { get; set; }
    public string? Pureza { get; set; }
    public string? Corte { get; set; }
    public string? Obs1 { get; set; }
    public string? Obs2 { get; set; }
    public decimal? Precio { get; set; }
    public string? NombreProveedor { get; set; }
}

// ── Piezas Compuestas ──
public class PiezaCompuesta
{
    public string CodigoBarras { get; set; } = "";
    public string? Descripcion { get; set; }
    public int? IdGrupo { get; set; }
    public string? EtiquetaK { get; set; }
    public string? Linea1 { get; set; }
    public string? Linea2 { get; set; }
    public string? Linea3 { get; set; }
    public int? Componentes { get; set; }
    public decimal? PrecioTotal { get; set; }
    public int? IdUsuario { get; set; }
    public DateTime? FechaCaptura { get; set; }
}

public class ComponenteCompuesta
{
    public string CodigoBarras { get; set; } = "";
    public string CBPadre { get; set; } = "";
    public int Indice { get; set; }
    public string? Descripcion { get; set; }
    public decimal? Precio { get; set; }
    public string? Kilates { get; set; }
    public string? Modelo { get; set; }
    public string? Linea { get; set; }
    public decimal? Quilates { get; set; }
    public string? Color { get; set; }
    public string? Pureza { get; set; }
    public string? Corte { get; set; }
    public string? Obs1 { get; set; }
    public string? Obs2 { get; set; }
    public string? NombreProveedor { get; set; }
    public string? NumSerie { get; set; }
}

// ── Inventario Físico ──
public class RegistroInventarioFisico
{
    public int IdRegistro { get; set; }
    public string CodigoBarras { get; set; } = "";
    public string? Descripcion { get; set; }
    public decimal? Precio { get; set; }
    public int? IdUsuario { get; set; }
    public DateTime? FechaCaptura { get; set; }
}

public class PiezaFaltante
{
    public string CodigoBarras { get; set; } = "";
    public string? Descripcion { get; set; }
    public decimal? Precio { get; set; }
    public string? NombreProveedor { get; set; }
    public string? Grupo { get; set; }
    public string? Comentario { get; set; }
}

public class PiezaSobrante
{
    public string CodigoBarras { get; set; } = "";
    public string? Descripcion { get; set; }
    public decimal? Precio { get; set; }
    public DateTime? FechaCaptura { get; set; }
}

// ── Transferencias ──
public class Transferencia
{
    public int IdTransferencia { get; set; }
    public string CodigoBarras { get; set; } = "";
    public string? Descripcion { get; set; }
    public int? TiendaOrigen { get; set; }
    public int? TiendaDestino { get; set; }
    public string? NombreTiendaOrigen { get; set; }
    public string? NombreTiendaDestino { get; set; }
    public int? IdUsuario { get; set; }
    public DateTime? FechaTransferencia { get; set; }
}

// ── Status de Piezas ──
public class StatusPieza
{
    public int IdStatus { get; set; }
    public string? NombreStatus { get; set; }
}

public class BitacoraStatus
{
    public int IdCambioStatus { get; set; }
    public string CodigoBarras { get; set; } = "";
    public int IdStatusAnterior { get; set; }
    public int IdStatusNuevo { get; set; }
    public string? StatusAnterior { get; set; }
    public string? StatusNuevo { get; set; }
    public int? IdUsuario { get; set; }
    public DateTime? FechaCambio { get; set; }
}

// ── Pre Bajas ──
public class PreBaja
{
    public string CodigoBarras { get; set; } = "";
    public int IdTipoBaja { get; set; }
    public string? TipoBaja => IdTipoBaja == 1 ? "Venta" : "Devolución";
    public string? Descripcion { get; set; }
    public DateTime? FechaCaptura { get; set; }
}

// ── Lotes Repetidas ──
public class LoteRepetida
{
    public int IdLote { get; set; }
    public string CodigoBarras { get; set; } = "";
    public string? Descripcion { get; set; }
    public int? Cantidad { get; set; }
    public decimal? Precio { get; set; }
    public int? Proveedor { get; set; }
    public string? NombreProveedor { get; set; }
    public int? IdRemision { get; set; }
    public int? IdFactura { get; set; }
    public decimal? CostoBruto { get; set; }
    public decimal? CostoNeto { get; set; }
    public decimal? Utilidad { get; set; }
    public decimal? UtilidadExtra { get; set; }
    public decimal? Impuesto { get; set; }
    public decimal? Divisor { get; set; }
    public int? IdMoneda { get; set; }
    public decimal? TCCosto { get; set; }
    public decimal? TCCotizacion { get; set; }
    public DateTime? FechaCaptura { get; set; }
}
