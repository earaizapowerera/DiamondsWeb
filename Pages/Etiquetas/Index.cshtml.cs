using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Etiquetas;

[Authorize]
public class IndexModel : PageModel
{
    private readonly EtiquetaService _etiquetaService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(EtiquetaService etiquetaService, ILogger<IndexModel> logger)
    {
        _etiquetaService = etiquetaService;
        _logger = logger;
    }

    public List<DisenoEtiqueta> Plantillas { get; set; } = new();
    public ConfiguracionEtiqueta Configuracion { get; set; } = new();
    public string? MensajeExito { get; set; }
    public string? MensajeError { get; set; }

    public async Task OnGetAsync()
    {
        await CargarDatosAsync();

        if (TempData["MensajeExito"] is string exito)
            MensajeExito = exito;
        if (TempData["MensajeError"] is string error)
            MensajeError = error;
    }

    /// <summary>
    /// Cambia la plantilla sencilla activa (equivale a Combo1_Click del VB6).
    /// </summary>
    public async Task<IActionResult> OnPostCambiarSencillaAsync(int idPlantilla)
    {
        try
        {
            await _etiquetaService.CambiarPlantillaSencillaAsync(idPlantilla);
            TempData["MensajeExito"] = "Plantilla de etiqueta sencilla actualizada.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar plantilla sencilla a {Id}", idPlantilla);
            TempData["MensajeError"] = "Error al cambiar la plantilla sencilla.";
        }
        return RedirectToPage();
    }

    /// <summary>
    /// Actualiza la plantilla compuesta (equivale a cmdActualizar_Click del VB6).
    /// </summary>
    public async Task<IActionResult> OnPostActualizarCompuestaAsync(string archivoCompuesta)
    {
        try
        {
            await _etiquetaService.ActualizarPlantillaCompuestaAsync(archivoCompuesta?.Trim() ?? "");
            TempData["MensajeExito"] = "Plantilla de etiqueta compuesta actualizada.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar plantilla compuesta");
            TempData["MensajeError"] = "Error al actualizar la plantilla compuesta.";
        }
        return RedirectToPage();
    }

    /// <summary>
    /// Agrega una nueva plantilla al catálogo.
    /// </summary>
    public async Task<IActionResult> OnPostAgregarPlantillaAsync(string nombrePlantilla)
    {
        if (string.IsNullOrWhiteSpace(nombrePlantilla))
        {
            TempData["MensajeError"] = "El nombre de la plantilla no puede estar vacío.";
            return RedirectToPage();
        }

        try
        {
            await _etiquetaService.AgregarPlantillaAsync(nombrePlantilla.Trim());
            TempData["MensajeExito"] = $"Plantilla '{nombrePlantilla.Trim()}' agregada al catálogo.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al agregar plantilla '{Nombre}'", nombrePlantilla);
            TempData["MensajeError"] = "Error al agregar la plantilla.";
        }
        return RedirectToPage();
    }

    /// <summary>
    /// Edita el nombre de una plantilla existente.
    /// </summary>
    public async Task<IActionResult> OnPostEditarPlantillaAsync(int idPlantilla, string nombrePlantilla)
    {
        if (string.IsNullOrWhiteSpace(nombrePlantilla))
        {
            TempData["MensajeError"] = "El nombre de la plantilla no puede estar vacío.";
            return RedirectToPage();
        }

        try
        {
            await _etiquetaService.ActualizarPlantillaAsync(idPlantilla, nombrePlantilla.Trim());
            TempData["MensajeExito"] = $"Plantilla actualizada a '{nombrePlantilla.Trim()}'.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al editar plantilla {Id}", idPlantilla);
            TempData["MensajeError"] = "Error al editar la plantilla.";
        }
        return RedirectToPage();
    }

    /// <summary>
    /// Elimina una plantilla del catálogo (no permite eliminar la activa).
    /// </summary>
    public async Task<IActionResult> OnPostEliminarPlantillaAsync(int idPlantilla)
    {
        try
        {
            var eliminada = await _etiquetaService.EliminarPlantillaAsync(idPlantilla);
            if (eliminada)
                TempData["MensajeExito"] = "Plantilla eliminada del catálogo.";
            else
                TempData["MensajeError"] = "No se puede eliminar la plantilla activa. Cambia primero la plantilla sencilla activa.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar plantilla {Id}", idPlantilla);
            TempData["MensajeError"] = "Error al eliminar la plantilla.";
        }
        return RedirectToPage();
    }

    private async Task CargarDatosAsync()
    {
        try
        {
            Plantillas = await _etiquetaService.ObtenerPlantillasAsync();
            Configuracion = await _etiquetaService.ObtenerConfiguracionAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar datos de etiquetas");
            MensajeError = "Error al cargar los datos de etiquetas. Verifique la conexión a la base de datos.";
        }
    }
}
