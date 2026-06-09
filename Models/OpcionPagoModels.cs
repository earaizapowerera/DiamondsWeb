namespace DiamondsWeb.Models;

/// <summary>
/// Moneda para dropdown de opciones de pago.
/// Nota: si MonedaModels.cs ya existe (de otro branch), esta clase se puede eliminar.
/// </summary>
public class MonedaItem
{
    public int IdMoneda { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Extranjera { get; set; }
}

/// <summary>
/// Opcion de pago — migrado de frmOpcionesPago.frm (VB6).
/// Tabla: OpcionesPago | Vista: vOpcionesPago.
/// </summary>
public class OpcionPago
{
    public int IdOpcionPago { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int IdMoneda { get; set; }
    public string? NombreMoneda { get; set; }
    public string? Logo { get; set; }
    public bool Activa { get; set; }
    public DateTime FechaCaptura { get; set; }
    public DateTime FechaUltEdicion { get; set; }
    public int IdUsuario { get; set; }
    public string? NombreUsuario { get; set; }

    /// <summary>
    /// Mapea el valor entero de Logo (legacy VB6) a clase Font Awesome.
    /// </summary>
    public static string LogoToFaIcon(string? logo)
    {
        return logo switch
        {
            "1" => "fa-brands fa-cc-amex",
            "2" => "fa-brands fa-cc-mastercard",
            "3" => "fa-brands fa-cc-visa",
            "4" => "fa-solid fa-money-bill-wave",
            "5" => "fa-solid fa-dollar-sign",
            "6" => "fa-solid fa-building-columns",
            "7" => "fa-solid fa-building-columns",
            "8" => "fa-solid fa-money-check",
            "9" => "fa-solid fa-building-columns",
            "10" => "fa-solid fa-building-columns",
            _ => "fa-solid fa-credit-card"
        };
    }

    /// <summary>
    /// Nombre descriptivo del icono para el dropdown.
    /// </summary>
    public static string LogoToNombre(string? logo)
    {
        return logo switch
        {
            "1" => "Amex",
            "2" => "MasterCard",
            "3" => "Visa",
            "4" => "Pesos",
            "5" => "Dolares",
            "6" => "Banamex",
            "7" => "CitiBank",
            "8" => "Cheque/Otro",
            "9" => "Bancomer",
            "10" => "Serfin",
            _ => "Generico"
        };
    }

    /// <summary>
    /// Lista de todos los iconos disponibles para el dropdown.
    /// </summary>
    public static List<(string Value, string Nombre, string FaIcon)> LogosDisponibles =>
    [
        ("1", "Amex", "fa-brands fa-cc-amex"),
        ("2", "MasterCard", "fa-brands fa-cc-mastercard"),
        ("3", "Visa", "fa-brands fa-cc-visa"),
        ("4", "Pesos", "fa-solid fa-money-bill-wave"),
        ("5", "Dolares", "fa-solid fa-dollar-sign"),
        ("6", "Banamex", "fa-solid fa-building-columns"),
        ("7", "CitiBank", "fa-solid fa-building-columns"),
        ("8", "Cheque / Otro", "fa-solid fa-money-check"),
        ("9", "Bancomer", "fa-solid fa-building-columns"),
        ("10", "Serfin", "fa-solid fa-building-columns")
    ];
}
