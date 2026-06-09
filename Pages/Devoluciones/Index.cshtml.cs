using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Devoluciones;

[Authorize]
public class IndexModel : PageModel
{
    private readonly DevolucionesService _service;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(DevolucionesService service, ILogger<IndexModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Resultado de la busqueda por codigo de barras</summary>
    public PiezaDevolucion? Pieza { get; set; }

    /// <summary>Tiendas disponibles para reestablecer</summary>
    public List<TiendaInfo> Tiendas { get; set; } = new();

    /// <summary>Mensaje de exito despues de reestablecer</summary>
    public string? MensajeExito { get; set; }

    /// <summary>Mensaje de error</summary>
    public string? MensajeError { get; set; }

    /// <summary>Indica si la pieza no fue encontrada (vs nunca busco)</summary>
    public bool BusquedaRealizada { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? CodigoBarras { get; set; }

    public async Task OnGetAsync()
    {
        Tiendas = await _service.ObtenerTiendasAsync();

        if (!string.IsNullOrWhiteSpace(CodigoBarras))
        {
            BusquedaRealizada = true;
            CodigoBarras = CodigoBarras.Trim();

            try
            {
                Pieza = await _service.BuscarPiezaAsync(CodigoBarras);
                if (Pieza == null)
                {
                    MensajeError = "El codigo tecleado no existe o la pieza esta en existencia. Vuelva a intentar.";
                }
                else if (!Pieza.EnBajas)
                {
                    // La pieza existe en piezasnotas pero ya no esta en bajaspiezas
                    var fechaPrevia = await _service.VerificarReestablecimientoPrevioAsync(CodigoBarras);
                    if (fechaPrevia.HasValue)
                    {
                        MensajeError = $"La pieza ya fue reestablecida el {fechaPrevia.Value:dd/MM/yyyy HH:mm}.";
                    }
                    else
                    {
                        MensajeError = "La pieza ya no esta en bajas. Puede que ya haya sido devuelta o este en existencia.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error buscando pieza {CB}", CodigoBarras);
                MensajeError = $"Error al buscar: {ex.Message}";
            }
        }
    }

    public async Task<IActionResult> OnPostReestablecerAsync(
        string codigoBarras, int idTienda)
    {
        var usuario = User.Identity?.Name ?? "admin";

        _logger.LogInformation(
            "Solicitud de reestablecimiento: CB={CB}, Tienda={Tienda}, Usuario={Usuario}",
            codigoBarras, idTienda, usuario);

        var resultado = await _service.ReestablecerPiezaAsync(codigoBarras, idTienda, usuario);

        if (resultado.Exito)
        {
            // Redirigir con mensaje de exito
            TempData["MensajeExito"] = resultado.Mensaje;
            return RedirectToPage(new { CodigoBarras = "" });
        }

        // Recargar la pagina con el error
        Tiendas = await _service.ObtenerTiendasAsync();
        CodigoBarras = codigoBarras;
        BusquedaRealizada = true;
        Pieza = await _service.BuscarPiezaAsync(codigoBarras);
        MensajeError = resultado.Mensaje;
        return Page();
    }
}
