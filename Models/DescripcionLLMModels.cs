namespace DiamondsWeb.Models;

/// <summary>
/// Configuracion del servicio de descripcion inteligente con Claude.
/// Se bindea desde appsettings.json seccion "LLMConfig".
/// </summary>
public class LLMConfig
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "claude-sonnet-4-20250514";
    public int MaxTokens { get; set; } = 300;

    /// <summary>Habilitar/deshabilitar el servicio sin quitar el codigo.</summary>
    public bool Habilitado { get; set; } = true;
}

/// <summary>
/// Request para mejorar descripcion de una pieza via LLM.
/// </summary>
public class MejorarDescripcionRequest
{
    public string Descripcion { get; set; } = "";
    public string? Grupo { get; set; }
    public string? Kilates { get; set; }
    public string? Modelo { get; set; }
    public string? Linea { get; set; }
    public decimal Quilates { get; set; }
    public string? Color { get; set; }
    public string? Pureza { get; set; }
    public string? Corte { get; set; }
    public string? NumSerie { get; set; }
    public string? Obs1 { get; set; }
    public string? Obs2 { get; set; }
    public string? DescripcionManoObra { get; set; }
    public string? Observaciones { get; set; }
    public decimal? Peso { get; set; }
    public string? TipoCaracteristica { get; set; } // Oro, Diamante, Reloj
}

/// <summary>
/// Resultado de la mejora de descripcion.
/// </summary>
public class MejorarDescripcionResult
{
    public bool Success { get; set; }
    public string DescripcionOriginal { get; set; } = "";
    public string DescripcionMejorada { get; set; } = "";
    public string? Error { get; set; }
}
