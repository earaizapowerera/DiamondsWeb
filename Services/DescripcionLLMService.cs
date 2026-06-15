using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DiamondsWeb.Models;

namespace DiamondsWeb.Services;

/// <summary>
/// Servicio que usa Claude (Anthropic) para mejorar descripciones de piezas de joyeria.
/// Funcionalidades:
///   1. Corrige ortografia
///   2. Hace la descripcion mas atractiva y profesional para el cliente
///   3. Si hay foto, usa vision para enriquecer la descripcion
/// Usa HttpClient directo contra la API de Anthropic (sin SDK externo).
/// </summary>
public class DescripcionLLMService
{
    private readonly LLMConfig _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DescripcionLLMService> _logger;

    private const string AnthropicApiUrl = "https://api.anthropic.com/v1/messages";
    private const string AnthropicVersion = "2023-06-01";

    public DescripcionLLMService(LLMConfig config, IHttpClientFactory httpClientFactory,
        ILogger<DescripcionLLMService> logger)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Mejora la descripcion de una pieza usando Claude.
    /// </summary>
    public async Task<MejorarDescripcionResult> MejorarDescripcionAsync(
        MejorarDescripcionRequest request, byte[]? fotoBytes = null, string? fotoMediaType = null)
    {
        if (!_config.Habilitado)
            return new MejorarDescripcionResult
            {
                Success = false,
                DescripcionOriginal = request.Descripcion,
                Error = "El servicio de descripcion inteligente esta deshabilitado"
            };

        if (string.IsNullOrWhiteSpace(_config.ApiKey))
            return new MejorarDescripcionResult
            {
                Success = false,
                DescripcionOriginal = request.Descripcion,
                Error = "API Key de Anthropic no configurada"
            };

        try
        {
            var systemPrompt = BuildSystemPrompt();
            var userPrompt = BuildUserPrompt(request);
            var content = BuildContent(userPrompt, fotoBytes, fotoMediaType);

            var apiRequest = new
            {
                model = _config.Model,
                max_tokens = _config.MaxTokens,
                system = systemPrompt,
                messages = new[]
                {
                    new { role = "user", content }
                }
            };

            var client = _httpClientFactory.CreateClient("Anthropic");
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-api-key", _config.ApiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", AnthropicVersion);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var json = JsonSerializer.Serialize(apiRequest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(AnthropicApiUrl, httpContent);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Anthropic API error {StatusCode}: {Body}",
                    response.StatusCode, responseBody);
                return new MejorarDescripcionResult
                {
                    Success = false,
                    DescripcionOriginal = request.Descripcion,
                    Error = $"Error de API ({response.StatusCode}). Intente de nuevo."
                };
            }

            var apiResponse = JsonSerializer.Deserialize<AnthropicResponse>(responseBody,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

            var descripcionMejorada = apiResponse?.Content?.FirstOrDefault()?.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(descripcionMejorada))
            {
                return new MejorarDescripcionResult
                {
                    Success = false,
                    DescripcionOriginal = request.Descripcion,
                    Error = "El modelo no genero una descripcion. Intente de nuevo."
                };
            }

            // Truncar a 100 chars (limite de la columna Descripcion en BD)
            if (descripcionMejorada.Length > 100)
                descripcionMejorada = descripcionMejorada[..100];

            _logger.LogInformation(
                "Descripcion mejorada: '{Original}' -> '{Mejorada}'",
                request.Descripcion, descripcionMejorada);

            return new MejorarDescripcionResult
            {
                Success = true,
                DescripcionOriginal = request.Descripcion,
                DescripcionMejorada = descripcionMejorada
            };
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Timeout al llamar Anthropic API");
            return new MejorarDescripcionResult
            {
                Success = false,
                DescripcionOriginal = request.Descripcion,
                Error = "Timeout al conectar con el servicio. Intente de nuevo."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado en DescripcionLLMService");
            return new MejorarDescripcionResult
            {
                Success = false,
                DescripcionOriginal = request.Descripcion,
                Error = "Error inesperado. Intente de nuevo."
            };
        }
    }

    private static string BuildSystemPrompt()
    {
        return """
            Eres un experto en joyeria fina (oro, diamantes, relojes de lujo).
            Tu tarea es mejorar la descripcion de una pieza de joyeria para que sea:
            1. Correcta ortograficamente (sin faltas)
            2. Atractiva y profesional para el cliente
            3. Concisa (maximo 100 caracteres)

            Reglas:
            - Responde SOLO con la descripcion mejorada, sin explicaciones ni comillas
            - Mantiene el idioma original (espanol)
            - Usa mayuscula inicial y minusculas despues
            - No inventes caracteristicas que no esten en la informacion proporcionada
            - Si hay foto, usala para enriquecer la descripcion con detalles visuales reales
            - Conserva informacion tecnica relevante (kilates, modelo, material)
            - Hazla atractiva para exhibicion en vitrina o catalogo
            """;
    }

    private static string BuildUserPrompt(MejorarDescripcionRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Descripcion actual: \"{request.Descripcion}\"");
        sb.AppendLine($"Tipo de pieza: {request.TipoCaracteristica ?? "General"}");

        if (!string.IsNullOrEmpty(request.Grupo))
            sb.AppendLine($"Grupo/Categoria: {request.Grupo}");

        // Caracteristicas segun tipo
        if (request.TipoCaracteristica == "Oro" || string.IsNullOrEmpty(request.TipoCaracteristica))
        {
            if (!string.IsNullOrEmpty(request.Kilates))
                sb.AppendLine($"Kilates: {request.Kilates}");
            if (!string.IsNullOrEmpty(request.Modelo))
                sb.AppendLine($"Modelo: {request.Modelo}");
            if (!string.IsNullOrEmpty(request.Linea))
                sb.AppendLine($"Linea: {request.Linea}");
            if (request.Peso > 0)
                sb.AppendLine($"Peso: {request.Peso:N2}g");
        }
        else if (request.TipoCaracteristica == "Diamante")
        {
            if (!string.IsNullOrEmpty(request.Kilates))
                sb.AppendLine($"Kilates (montura): {request.Kilates}");
            if (request.Quilates > 0)
                sb.AppendLine($"Quilates (diamante): {request.Quilates:N2}");
            if (!string.IsNullOrEmpty(request.Color))
                sb.AppendLine($"Color: {request.Color}");
            if (!string.IsNullOrEmpty(request.Pureza))
                sb.AppendLine($"Pureza: {request.Pureza}");
            if (!string.IsNullOrEmpty(request.Corte))
                sb.AppendLine($"Corte: {request.Corte}");
        }
        else if (request.TipoCaracteristica == "Reloj")
        {
            if (!string.IsNullOrEmpty(request.Modelo))
                sb.AppendLine($"Modelo: {request.Modelo}");
            if (!string.IsNullOrEmpty(request.Linea))
                sb.AppendLine($"Linea: {request.Linea}");
            if (!string.IsNullOrEmpty(request.NumSerie))
                sb.AppendLine($"Numero de serie: {request.NumSerie}");
        }

        if (!string.IsNullOrEmpty(request.DescripcionManoObra))
            sb.AppendLine($"Mano de obra: {request.DescripcionManoObra}");
        if (!string.IsNullOrEmpty(request.Observaciones))
            sb.AppendLine($"Observaciones: {request.Observaciones}");
        if (!string.IsNullOrEmpty(request.Obs1))
            sb.AppendLine($"Obs1: {request.Obs1}");
        if (!string.IsNullOrEmpty(request.Obs2))
            sb.AppendLine($"Obs2: {request.Obs2}");

        sb.AppendLine();
        sb.AppendLine("Genera una descripcion mejorada (maximo 100 caracteres):");
        return sb.ToString();
    }

    /// <summary>
    /// Construye el array de content blocks para el mensaje.
    /// Si hay foto, incluye un bloque de imagen (vision) + texto.
    /// Si no hay foto, solo texto.
    /// </summary>
    private static object[] BuildContent(string userPrompt, byte[]? fotoBytes, string? fotoMediaType)
    {
        if (fotoBytes != null && fotoBytes.Length > 0 && !string.IsNullOrEmpty(fotoMediaType))
        {
            var base64 = Convert.ToBase64String(fotoBytes);
            return new object[]
            {
                new
                {
                    type = "image",
                    source = new
                    {
                        type = "base64",
                        media_type = fotoMediaType,
                        data = base64
                    }
                },
                new
                {
                    type = "text",
                    text = userPrompt + "\n\nLa imagen adjunta muestra la pieza real. Usala para enriquecer la descripcion con detalles visuales."
                }
            };
        }

        return new object[]
        {
            new { type = "text", text = userPrompt }
        };
    }

    // ==================== DTOs para deserializar respuesta de Anthropic ====================

    private class AnthropicResponse
    {
        public string? Id { get; set; }
        public string? Model { get; set; }
        public List<ContentBlock>? Content { get; set; }
        public string? StopReason { get; set; }
        public UsageInfo? Usage { get; set; }
    }

    private class ContentBlock
    {
        public string? Type { get; set; }
        public string? Text { get; set; }
    }

    private class UsageInfo
    {
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
    }
}
