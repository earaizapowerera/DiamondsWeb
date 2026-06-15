using System.Security.Claims;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiamondsWeb.Controllers;

/// <summary>
/// API Controller para fotos de piezas.
/// Endpoints autenticados via cookie (UserPortal).
/// Usado por la PWA movil y la pagina web de Alta de Piezas.
/// </summary>
[ApiController]
[Route("api/fotos")]
[Authorize]
public class FotosController : ControllerBase
{
    private readonly FotoService _fotoService;

    public FotosController(FotoService fotoService) => _fotoService = fotoService;

    private int GetUserId()
    {
        var claim = User.FindFirst("IdUsuario")?.Value;
        if (int.TryParse(claim, out var uid) && uid > 0) return uid;
        throw new UnauthorizedAccessException("IdUsuario claim not found");
    }

    /// <summary>
    /// Subir foto (desde PWA movil o navegador web).
    /// POST /api/fotos/upload
    /// Content-Type: multipart/form-data
    /// Fields: file (required), source (optional: "mobile"|"web", default "mobile")
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] string? source = "mobile")
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No se envio archivo" });

        var userId = GetUserId();
        using var stream = file.OpenReadStream();
        var result = await _fotoService.SubirFotoAsync(
            stream, file.FileName, file.ContentType, file.Length, userId, source ?? "mobile");

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
    /// Listar ultimas N fotos no vinculadas del usuario autenticado.
    /// GET /api/fotos/recientes?count=3&source=mobile
    /// </summary>
    [HttpGet("recientes")]
    public async Task<IActionResult> Recientes([FromQuery] int count = 3, [FromQuery] string? source = null)
    {
        var userId = GetUserId();
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
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _fotoService.EliminarFotoAsync(id);
        if (!ok)
            return NotFound(new { error = "Foto no encontrada" });

        return Ok(new { success = true });
    }
}
