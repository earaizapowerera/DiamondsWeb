using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiamondsWeb.Controllers;

/// <summary>
/// API Controller para fotos de piezas.
/// Expone endpoints para la app movil Diamonds y consultas internas.
/// </summary>
[ApiController]
[Route("api/fotos")]
public class FotosController : ControllerBase
{
    private readonly FotoService _fotoService;
    private readonly string _apiKey;

    public FotosController(FotoService fotoService, IConfiguration config)
    {
        _fotoService = fotoService;
        _apiKey = config["Diamonds:MobileApiKey"] ?? string.Empty;
    }

    /// <summary>
    /// Valida API key del header X-Api-Key para endpoints de la app móvil.
    /// </summary>
    private bool ValidarApiKey()
    {
        if (string.IsNullOrEmpty(_apiKey))
            return false;
        var headerKey = Request.Headers["X-Api-Key"].FirstOrDefault();
        return !string.IsNullOrEmpty(headerKey) && headerKey == _apiKey;
    }

    /// <summary>
    /// Subir foto desde la app movil Diamonds.
    /// POST /api/fotos/upload
    /// Content-Type: multipart/form-data
    /// Headers: X-Api-Key (required)
    /// Fields: file (required), userId (required)
    /// </summary>
    [HttpPost("upload")]
    [AllowAnonymous]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] int userId)
    {
        if (!ValidarApiKey() && !User.Identity?.IsAuthenticated == true)
            return Unauthorized(new { error = "API key invalida o sesion no autenticada" });

        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No se envio archivo" });

        if (userId <= 0)
            return BadRequest(new { error = "userId es requerido" });

        using var stream = file.OpenReadStream();
        var result = await _fotoService.SubirFotoAsync(
            stream, file.FileName, file.ContentType, file.Length, userId, "mobile");

        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(new
        {
            id = result.FotoId,
            url = result.Url,
            storedFileName = result.StoredFileName
        });
    }

    /// <summary>
    /// Listar ultimas N fotos no vinculadas de un usuario.
    /// GET /api/fotos/recientes?userId=1&count=3&source=mobile
    /// Headers: X-Api-Key (required for mobile), or session auth
    /// </summary>
    [HttpGet("recientes")]
    [AllowAnonymous]
    public async Task<IActionResult> Recientes([FromQuery] int userId, [FromQuery] int count = 3, [FromQuery] string? source = null)
    {
        if (!ValidarApiKey() && !User.Identity?.IsAuthenticated == true)
            return Unauthorized(new { error = "API key invalida o sesion no autenticada" });

        if (userId <= 0)
            return BadRequest(new { error = "userId es requerido" });

        var fotos = await _fotoService.ObtenerFotosRecientesAsync(userId, count, source);
        return Ok(fotos.Select(f => new
        {
            f.Id,
            f.Url,
            f.FileName,
            f.Source,
            uploadedAt = f.UploadedAt.ToString("yyyy-MM-dd HH:mm")
        }));
    }

    /// <summary>
    /// Obtener info de una foto especifica.
    /// GET /api/fotos/{id}
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> Get(int id)
    {
        var foto = await _fotoService.ObtenerFotoAsync(id);
        if (foto == null)
            return NotFound(new { error = "Foto no encontrada" });

        return Ok(new
        {
            foto.Id,
            foto.Url,
            foto.FileName,
            foto.Source,
            foto.CodigoBarras,
            foto.IsLinked,
            uploadedAt = foto.UploadedAt.ToString("yyyy-MM-dd HH:mm")
        });
    }

    /// <summary>
    /// Eliminar una foto.
    /// DELETE /api/fotos/{id}
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _fotoService.EliminarFotoAsync(id);
        if (!ok)
            return NotFound(new { error = "Foto no encontrada" });

        return Ok(new { success = true });
    }
}
