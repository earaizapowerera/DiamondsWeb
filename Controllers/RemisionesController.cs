using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiamondsWeb.Controllers;

[Route("api/remisiones")]
[ApiController]
[Authorize]
public class RemisionesController : ControllerBase
{
    private readonly RemisionService _remisionService;

    public RemisionesController(RemisionService remisionService)
    {
        _remisionService = remisionService;
    }

    [HttpGet]
    public async Task<IActionResult> Buscar(
        [FromQuery] string? buscar,
        [FromQuery] int? proveedorId,
        [FromQuery] bool? soloConsignacion)
    {
        var remisiones = await _remisionService.BuscarRemisionesAsync(buscar, proveedorId, soloConsignacion);
        return Ok(remisiones);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        var remision = await _remisionService.ObtenerRemisionAsync(id);
        if (remision == null) return NotFound("Remisión no encontrada");
        return Ok(remision);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearRemisionRequest request)
    {
        if (request.Proveedor <= 0) return BadRequest("El proveedor es requerido");
        if (string.IsNullOrWhiteSpace(request.Remision)) return BadRequest("El número de remisión es requerido");

        var idRemision = await _remisionService.CrearRemisionAsync(
            request.Proveedor, request.Remision, request.FechaRemision,
            request.Consignacion, 1);

        return Ok(new { message = "Remisión creada exitosamente.", idRemision });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarRemisionRequest request)
    {
        await _remisionService.ActualizarRemisionAsync(
            id, request.Proveedor, request.Remision,
            request.FechaRemision, request.Consignacion);

        return Ok(new { message = "Remisión actualizada exitosamente." });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var eliminada = await _remisionService.EliminarRemisionAsync(id);
        if (!eliminada)
            return BadRequest("No se puede eliminar: la remisión tiene piezas vinculadas. Desvincule las piezas primero.");

        return Ok(new { message = "Remisión eliminada exitosamente." });
    }

    // Piezas disponibles
    [HttpGet("{id:int}/piezas-disponibles")]
    public async Task<IActionResult> PiezasDisponibles(int id, [FromQuery] string? buscar)
    {
        var piezas = await _remisionService.ObtenerPiezasDisponiblesAsync(id, buscar);
        return Ok(piezas);
    }

    // Piezas asignadas
    [HttpGet("{id:int}/piezas")]
    public async Task<IActionResult> PiezasRemision(int id)
    {
        var piezas = await _remisionService.ObtenerPiezasRemisionAsync(id);
        return Ok(piezas);
    }

    // Totales
    [HttpGet("{id:int}/totales")]
    public async Task<IActionResult> Totales(int id)
    {
        var totales = await _remisionService.ObtenerTotalesRemisionAsync(id);
        return Ok(totales);
    }

    // Vincular pieza
    [HttpPost("{id:int}/vincular")]
    public async Task<IActionResult> VincularPieza(int id, [FromBody] VincularPiezaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CodigoBarras))
            return BadRequest("El código de barras es requerido");

        await _remisionService.VincularPiezaAsync(id, request.CodigoBarras);
        return Ok(new { message = "Pieza vinculada exitosamente." });
    }

    // Desvincular pieza
    [HttpPost("{id:int}/desvincular")]
    public async Task<IActionResult> DesvincularPieza(int id, [FromBody] VincularPiezaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CodigoBarras))
            return BadRequest("El código de barras es requerido");

        await _remisionService.DesvincularPiezaAsync(request.CodigoBarras);
        return Ok(new { message = "Pieza desvinculada exitosamente." });
    }

    // Proveedores
    [HttpGet("proveedores")]
    public async Task<IActionResult> Proveedores([FromQuery] string? buscar)
    {
        var proveedores = string.IsNullOrWhiteSpace(buscar)
            ? await _remisionService.ObtenerProveedoresAsync()
            : await _remisionService.BuscarProveedoresAsync(buscar);
        return Ok(proveedores);
    }

    public class CrearRemisionRequest
    {
        public int Proveedor { get; set; }
        public string Remision { get; set; } = "";
        public DateTime? FechaRemision { get; set; }
        public bool Consignacion { get; set; }
    }

    public class ActualizarRemisionRequest
    {
        public int Proveedor { get; set; }
        public string Remision { get; set; } = "";
        public DateTime? FechaRemision { get; set; }
        public bool Consignacion { get; set; }
    }

    public class VincularPiezaRequest
    {
        public string CodigoBarras { get; set; } = "";
    }
}
