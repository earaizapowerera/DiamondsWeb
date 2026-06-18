using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio para mejorar descripciones de piezas de joyeria usando LLM (OpenAI GPT-4o-mini).
/// Soporta texto plano y vision (foto de la pieza).
/// </summary>
public class LlmService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LlmService> _logger;
    private readonly string _fotosPath;

    private const string ApiUrl = "https://api.openai.com/v1/chat/completions";
    private const string Model = "gpt-4o-mini";
    private const int MaxDescripcionChars = 100;

    private const string SystemPrompt = @"Eres un redactor experto de una joyeria de alta gama llamada Diamonds.
Tu trabajo es mejorar descripciones de piezas de joyeria para que sean atractivas, profesionales y sin faltas de ortografia.

Reglas ESTRICTAS:
- La descripcion DEBE tener maximo 100 caracteres (es un campo corto de base de datos)
- Corrige ortografia y acentos
- Hazla atractiva y comercial pero concisa
- Usa mayusculas solo donde corresponda
- NO uses comillas, ni emojis, ni signos de exclamacion
- Si recibes una foto, usala para identificar el tipo de pieza y generar una descripcion mas precisa
- Responde UNICAMENTE con la descripcion mejorada, sin explicaciones ni texto adicional
- Si la descripcion original esta vacia y hay foto, genera una descripcion basada en la imagen";

    public LlmService(HttpClient httpClient, string fotosPath, ILogger<LlmService> logger)
    {
        _httpClient = httpClient;
        _fotosPath = fotosPath;
        _logger = logger;
    }

    /// <summary>
    /// Mejora la descripcion de una pieza usando LLM.
    /// Si se proporciona archivoFoto (StoredFileName), se envia la imagen al modelo de vision.
    /// </summary>
    public async Task<LlmDescripcionResult> MejorarDescripcionAsync(
        string descripcionOriginal, string? archivoFoto = null, string? grupo = null)
    {
        try
        {
            var contenidoUsuario = new List<object>();

            // Construir prompt del usuario
            var textoPrompt = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(descripcionOriginal))
                textoPrompt.Append($"Descripcion actual: {descripcionOriginal}");
            else
                textoPrompt.Append("No hay descripcion. Genera una basada en la imagen.");

            if (!string.IsNullOrWhiteSpace(grupo))
                textoPrompt.Append($"\nGrupo/categoria: {grupo}");

            contenidoUsuario.Add(new { type = "text", text = textoPrompt.ToString() });

            // Si hay foto, leerla y adjuntarla como base64
            if (!string.IsNullOrWhiteSpace(archivoFoto))
            {
                var fotoPath = Path.Combine(_fotosPath, archivoFoto);
                if (File.Exists(fotoPath))
                {
                    var bytes = await File.ReadAllBytesAsync(fotoPath);
                    var base64 = Convert.ToBase64String(bytes);
                    var ext = Path.GetExtension(archivoFoto).ToLower().TrimStart('.');
                    var mimeType = ext switch
                    {
                        "jpg" or "jpeg" => "image/jpeg",
                        "png" => "image/png",
                        "webp" => "image/webp",
                        "gif" => "image/gif",
                        _ => "image/jpeg"
                    };

                    contenidoUsuario.Add(new
                    {
                        type = "image_url",
                        image_url = new { url = $"data:{mimeType};base64,{base64}", detail = "low" }
                    });

                    _logger.LogInformation("LLM: Enviando imagen {Foto} ({Size} KB)", archivoFoto, bytes.Length / 1024);
                }
                else
                {
                    _logger.LogWarning("LLM: Foto no encontrada en disco: {Foto}", fotoPath);
                }
            }

            var requestBody = new
            {
                model = Model,
                max_tokens = 150,
                temperature = 0.7,
                messages = new object[]
                {
                    new { role = "system", content = SystemPrompt },
                    new { role = "user", content = contenidoUsuario }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(ApiUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("LLM API error {Status}: {Body}", response.StatusCode, responseBody);
                return new LlmDescripcionResult
                {
                    Success = false,
                    Error = $"Error del servicio de IA ({response.StatusCode})"
                };
            }

            var result = JsonSerializer.Deserialize<OpenAiResponse>(responseBody);
            var descripcionMejorada = result?.Choices?.FirstOrDefault()?.Message?.Content?.Trim() ?? "";

            // Truncar a 100 chars por seguridad
            if (descripcionMejorada.Length > MaxDescripcionChars)
                descripcionMejorada = descripcionMejorada[..MaxDescripcionChars];

            _logger.LogInformation("LLM: '{Original}' -> '{Mejorada}'",
                descripcionOriginal, descripcionMejorada);

            return new LlmDescripcionResult
            {
                Success = true,
                DescripcionMejorada = descripcionMejorada,
                DescripcionOriginal = descripcionOriginal
            };
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("LLM: Timeout al llamar API");
            return new LlmDescripcionResult { Success = false, Error = "Tiempo de espera agotado" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM: Error inesperado");
            return new LlmDescripcionResult { Success = false, Error = "Error interno al mejorar descripcion" };
        }
    }
}

// ==================== DTOs ====================

public class LlmDescripcionResult
{
    public bool Success { get; set; }
    public string? DescripcionMejorada { get; set; }
    public string? DescripcionOriginal { get; set; }
    public string? Error { get; set; }
}

// OpenAI response mapping
public class OpenAiResponse
{
    [JsonPropertyName("choices")]
    public List<OpenAiChoice>? Choices { get; set; }
}

public class OpenAiChoice
{
    [JsonPropertyName("message")]
    public OpenAiMessage? Message { get; set; }
}

public class OpenAiMessage
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}
