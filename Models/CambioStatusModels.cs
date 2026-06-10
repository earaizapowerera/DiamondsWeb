namespace DiamondsWeb.Models;

/// <summary>Catálogo de status de piezas (tabla statuspiezas)</summary>
public class StatusPieza
{
    public int IdStatus { get; set; }
    public string NombreStatus { get; set; } = "";
}

/// <summary>Info de pieza encontrada por código de barras</summary>
public class PiezaStatus
{
    public string CodigoBarras { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public int IdStatus { get; set; }
    public string NombreStatus { get; set; } = "";
    public DateTime? FechaUltimoCambio { get; set; }
}

/// <summary>Fila del grid vpiezasenstatus (piezas fuera de Exhibición)</summary>
public class PiezaEnStatus
{
    public string CodigoBarras { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string NombreStatus { get; set; } = "";
    public DateTime? UltimoCambio { get; set; }
}

/// <summary>Registro de bitácora de cambios de status</summary>
public class BitacoraStatus
{
    public int IdCambioStatus { get; set; }
    public string CodigoBarras { get; set; } = "";
    public int? IdStatusAnterior { get; set; }
    public string? NombreStatusAnterior { get; set; }
    public int? IdStatusNuevo { get; set; }
    public string? NombreStatusNuevo { get; set; }
    public DateTime FechaCaptura { get; set; }
    public int? IdUsuario { get; set; }
    // Aliases used by Razor views
    public string? StatusAnterior => NombreStatusAnterior;
    public string? StatusNuevo => NombreStatusNuevo;
    public DateTime? FechaCambio => FechaCaptura;
}
