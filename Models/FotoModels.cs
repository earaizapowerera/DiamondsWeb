namespace DiamondsWeb.Models;

// ========== DTOs para Fotos de Piezas ==========

/// <summary>
/// Foto de pieza almacenada en PiezasFotos.
/// Puede originarse desde el navegador web o la app movil Diamonds.
/// </summary>
public class PiezaFoto
{
    public int Id { get; set; }
    public string FileName { get; set; } = "";
    public string StoredFileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long FileSize { get; set; }
    public int UserId { get; set; }
    public DateTime UploadedAt { get; set; }
    public string Source { get; set; } = "web"; // "web" or "mobile"
    public string? CodigoBarras { get; set; }
    public bool IsLinked { get; set; }

    /// <summary>URL relativa para mostrar la foto en el navegador</summary>
    public string Url => $"/fotos-piezas/{StoredFileName}";
}

/// <summary>
/// Resultado de subir una foto
/// </summary>
public class SubirFotoResult
{
    public bool Success { get; set; }
    public int? FotoId { get; set; }
    public string? Url { get; set; }
    public string? StoredFileName { get; set; }
    public string? Error { get; set; }
}
