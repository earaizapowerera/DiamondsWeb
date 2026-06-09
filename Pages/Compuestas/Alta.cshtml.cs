using System.Text.Json;
using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Compuestas;

[Authorize]
public class AltaModel : PageModel
{
    private readonly CompuestaService _service;
    private readonly ILogger<AltaModel> _logger;

    public AltaModel(CompuestaService service, ILogger<AltaModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    public bool EsEdicion => !string.IsNullOrEmpty(CodigoBarras);
    public string? ErrorMessage { get; set; }
    public List<GrupoCatalogo> Grupos { get; set; } = new();
    public List<ComponenteDetalle> Componentes { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? CB { get; set; }

    // Datos del formulario
    public string? CodigoBarras { get; set; }

    [BindProperty]
    public string Descripcion { get; set; } = "";

    [BindProperty]
    public int IdGrupo { get; set; }

    [BindProperty]
    public int EtiquetaK { get; set; }

    [BindProperty]
    public int Linea1 { get; set; }

    [BindProperty]
    public int Linea2 { get; set; }

    [BindProperty]
    public int Linea3 { get; set; }

    /// <summary>
    /// JSON string con los códigos de barras de los componentes
    /// </summary>
    [BindProperty]
    public string ComponentesJson { get; set; } = "[]";

    /// <summary>
    /// CB de la compuesta en edición (hidden field)
    /// </summary>
    [BindProperty]
    public string? EditCB { get; set; }

    public async Task OnGetAsync()
    {
        Grupos = await _service.ObtenerGruposAsync();

        if (!string.IsNullOrEmpty(CB))
        {
            var detalle = await _service.ObtenerDetalleAsync(CB);
            if (detalle != null)
            {
                CodigoBarras = detalle.CodigoBarras;
                EditCB = detalle.CodigoBarras;
                Descripcion = detalle.Descripcion;
                IdGrupo = detalle.IdGrupo;
                EtiquetaK = detalle.EtiquetaK;
                Linea1 = detalle.Linea1;
                Linea2 = detalle.Linea2;
                Linea3 = detalle.Linea3;
                Componentes = detalle.ListaComponentes;
                ComponentesJson = JsonSerializer.Serialize(
                    detalle.ListaComponentes.Select(c => c.CodigoBarras).ToList());
            }
            else
            {
                ErrorMessage = $"No se encontro la compuesta {CB}";
            }
        }
    }

    public async Task<IActionResult> OnPostGuardarAsync()
    {
        Grupos = await _service.ObtenerGruposAsync();

        if (string.IsNullOrWhiteSpace(Descripcion))
        {
            ErrorMessage = "La descripcion es obligatoria.";
            await RecargarComponentes();
            return Page();
        }

        var componentesCB = new List<string>();
        try
        {
            componentesCB = JsonSerializer.Deserialize<List<string>>(ComponentesJson ?? "[]") ?? new();
        }
        catch
        {
            ErrorMessage = "Error al procesar los componentes.";
            return Page();
        }

        if (componentesCB.Count < 2)
        {
            ErrorMessage = "Una pieza compuesta necesita al menos 2 componentes.";
            await RecargarComponentes();
            return Page();
        }

        var req = new CompuestaRequest
        {
            CodigoBarras = EditCB,
            Descripcion = Descripcion.Trim(),
            IdGrupo = IdGrupo,
            EtiquetaK = EtiquetaK,
            Linea1 = Linea1,
            Linea2 = Linea2,
            Linea3 = Linea3,
            ComponentesCB = componentesCB
        };

        try
        {
            if (string.IsNullOrEmpty(EditCB))
            {
                var nuevoCB = await _service.CrearCompuestaAsync(req, idUsuario: 1, idTienda: 1);
                _logger.LogInformation("Compuesta creada: {CB}", nuevoCB);
                return RedirectToPage("Index");
            }
            else
            {
                await _service.ActualizarCompuestaAsync(req, idUsuario: 1);
                _logger.LogInformation("Compuesta actualizada: {CB}", EditCB);
                return RedirectToPage("Index");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error guardando compuesta");
            ErrorMessage = $"Error al guardar: {ex.Message}";
            await RecargarComponentes();
            return Page();
        }
    }

    /// <summary>
    /// API endpoint para buscar pieza disponible (AJAX)
    /// </summary>
    public async Task<IActionResult> OnGetBuscarPiezaAsync(string cb)
    {
        var pieza = await _service.BuscarPiezaDisponibleAsync(cb, CB);
        if (pieza == null)
            return new JsonResult(new { ok = false, error = "Pieza no encontrada o ya es componente de otra compuesta." });

        return new JsonResult(new
        {
            ok = true,
            data = new
            {
                pieza.CodigoBarras,
                pieza.Descripcion,
                pieza.Kilates,
                pieza.Modelo,
                pieza.Linea,
                pieza.Quilates,
                pieza.Color,
                pieza.Pureza,
                pieza.Corte,
                pieza.Obs1,
                pieza.Obs2,
                pieza.Precio,
                pieza.Proveedor,
                pieza.NumSerie
            }
        });
    }

    private async Task RecargarComponentes()
    {
        // Recargar componentes desde JSON para mostrar en la vista
        try
        {
            var cbs = JsonSerializer.Deserialize<List<string>>(ComponentesJson ?? "[]") ?? new();
            Componentes = new();
            foreach (var cb in cbs)
            {
                var pieza = await _service.BuscarPiezaDisponibleAsync(cb, EditCB);
                if (pieza != null)
                    Componentes.Add(pieza);
            }
        }
        catch { /* componentes quedan vacíos */ }

        CodigoBarras = EditCB;
    }
}
