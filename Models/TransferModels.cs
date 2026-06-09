namespace DiamondsWeb.Models;

/// <summary>
/// Tienda con su IdTienda y nombre
/// </summary>
public class Tienda
{
    public int IdTienda { get; set; }
    public string NombreTienda { get; set; } = "";
}

/// <summary>
/// Pieza individual en tránsito (sencilla o compuesta)
/// </summary>
public class PiezaEnTransito
{
    public string CodigoBarras { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public int IdLocalizacion { get; set; }
    public string NombreLocalizacion { get; set; } = "";
    public int Precio { get; set; }
    public string? Proveedor { get; set; }
    public DateTime? FechaUltEdicion { get; set; }
    public string TipoPieza { get; set; } = "Sencilla";
}

/// <summary>
/// Lote de piezas repetidas en tránsito
/// </summary>
public class LoteEnTransito
{
    public int IdLote { get; set; }
    public string CodigoBarras { get; set; } = "";
    public int Cantidad { get; set; }
    public string Descripcion { get; set; } = "";
    public string NombreLocalizacion { get; set; } = "";
    public int IdTienda { get; set; }
}

/// <summary>
/// Resultado de una operación de transferencia
/// </summary>
public class TransferResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";

    public static TransferResult Ok(string message) => new() { Success = true, Message = message };
    public static TransferResult Error(string message) => new() { Success = false, Message = message };
}

/// <summary>
/// Registro del log de transferencias
/// </summary>
public class LogTransferencia
{
    public string CodigoBarras { get; set; } = "";
    public int LocalizacionOrigen { get; set; }
    public string NombreOrigen { get; set; } = "";
    public int LocalizacionDestino { get; set; }
    public string NombreDestino { get; set; } = "";
    public int IdUsuario { get; set; }
    public DateTime FechaCaptura { get; set; }
    public int Cantidad { get; set; }
}
