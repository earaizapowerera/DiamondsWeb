using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiamondsWeb.Controllers;

/// <summary>
/// API Controller para configuraciones de columnas de grids.
/// Expone CRUD sobre las tablas legacy TablasColumnas/Columnas.
/// </summary>
[ApiController]
[Route("api/column-config")]
[Authorize]
public class ColumnConfigController : ControllerBase
{
    private readonly ColumnaConfigService _svc;

    public ColumnConfigController(ColumnaConfigService svc) => _svc = svc;

    /// <summary>
    /// Lista las configuraciones guardadas para una vista.
    /// GET /api/column-config?vista=vPiezas
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string vista)
    {
        if (string.IsNullOrWhiteSpace(vista))
            return BadRequest(new { error = "El parámetro 'vista' es requerido" });

        var configs = await _svc.ObtenerConfiguracionesAsync(vista);
        return Ok(configs);
    }

    /// <summary>
    /// Obtiene una configuración específica con sus columnas.
    /// GET /api/column-config/155
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var config = await _svc.ObtenerConfiguracionAsync(id);
        if (config == null)
            return NotFound(new { error = "Configuración no encontrada" });

        return Ok(config);
    }

    /// <summary>
    /// Crea una nueva configuración de columnas.
    /// POST /api/column-config
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CrearColumnaConfigRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Descripcion))
            return BadRequest(new { error = "La descripción es requerida" });
        if (string.IsNullOrWhiteSpace(request.Vista))
            return BadRequest(new { error = "La vista es requerida" });
        if (request.Columnas.Count == 0)
            return BadRequest(new { error = "Debe incluir al menos una columna" });

        var id = await _svc.CrearConfiguracionAsync(request);
        return Created($"/api/column-config/{id}", new { idTablaColumnas = id });
    }

    /// <summary>
    /// Elimina una configuración de columnas.
    /// DELETE /api/column-config/155
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _svc.EliminarConfiguracionAsync(id);
        if (!deleted)
            return NotFound(new { error = "Configuración no encontrada" });

        return Ok(new { message = "Configuración eliminada" });
    }
}
