using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiamondsWeb.Controllers;

/// <summary>
/// API para gestionar configuraciones de columnas visibles por usuario.
/// Usado via AJAX desde las paginas de listado de piezas.
/// </summary>
[ApiController]
[Route("api/columnas")]
[Authorize]
public class ColumnConfigController : ControllerBase
{
    private readonly ColumnConfigService _svc;
    private readonly ILogger<ColumnConfigController> _logger;

    public ColumnConfigController(ColumnConfigService svc, ILogger<ColumnConfigController> logger)
    {
        _svc = svc;
        _logger = logger;
    }

    private int GetUserId()
    {
        var claim = User.FindFirst("IdUsuario")?.Value;
        return int.TryParse(claim, out var id) ? id : 1;
    }

    /// <summary>
    /// Obtiene la configuracion de columnas del usuario para una vista.
    /// GET /api/columnas/{vista}
    /// </summary>
    [HttpGet("{vista}")]
    public async Task<IActionResult> ObtenerConfiguracion(string vista)
    {
        try
        {
            var idUsuario = GetUserId();
            var config = await _svc.ObtenerConfiguracionUsuarioAsync(idUsuario, vista);
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener configuracion de columnas para vista {Vista}", vista);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Guarda la configuracion de columnas del usuario.
    /// POST /api/columnas/guardar
    /// </summary>
    [HttpPost("guardar")]
    public async Task<IActionResult> GuardarConfiguracion([FromBody] GuardarColumnasRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Vista))
                return BadRequest(new { error = "Vista es requerida" });

            if (request.ColumnasVisibles == null || request.ColumnasVisibles.Count == 0)
                return BadRequest(new { error = "Debe seleccionar al menos una columna" });

            var idUsuario = GetUserId();
            var id = await _svc.GuardarConfiguracionAsync(
                idUsuario, request.Vista, request.Descripcion, request.ColumnasVisibles);

            return Ok(new { idTablaColumnas = id, mensaje = "Configuracion guardada" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar configuracion de columnas");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Elimina la configuracion del usuario (vuelve a defaults).
    /// DELETE /api/columnas/{vista}
    /// </summary>
    [HttpDelete("{vista}")]
    public async Task<IActionResult> EliminarConfiguracion(string vista)
    {
        try
        {
            var idUsuario = GetUserId();
            var result = await _svc.EliminarConfiguracionAsync(idUsuario, vista);
            return Ok(new { eliminado = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar configuracion de columnas para vista {Vista}", vista);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
