using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.AntiLavado;

[Authorize]
public class HomologacionModel : PageModel
{
    private readonly HomologacionService _service;
    private readonly ILogger<HomologacionModel> _logger;

    public HomologacionModel(HomologacionService service, ILogger<HomologacionModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    public List<GrupoHomologacion> GruposPendientes { get; set; } = new();
    public List<GrupoHomologacion> GruposAprobados { get; set; } = new();
    public HomologacionStats Stats { get; set; } = new();
    public string? Mensaje { get; set; }
    public string? Error { get; set; }
    public int Pagina { get; set; } = 1;
    public int TotalGrupos { get; set; }
    public int PageSize { get; set; } = 20;

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Tab { get; set; } = "pendientes";

    public async Task OnGetAsync(int pagina = 1)
    {
        Pagina = pagina;
        await CargarDatosAsync();
    }

    /// <summary>
    /// Ejecutar detección de duplicados
    /// </summary>
    public async Task<IActionResult> OnPostDetectarAsync()
    {
        try
        {
            var resultado = await _service.DetectarDuplicadosAsync();
            Mensaje = resultado.Mensaje;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al detectar duplicados");
            Error = $"Error al detectar duplicados: {ex.Message}";
        }

        await CargarDatosAsync();
        return Page();
    }

    /// <summary>
    /// Aprobar un grupo
    /// </summary>
    public async Task<IActionResult> OnPostAprobarAsync(int grupoId, string? nombreCanonical)
    {
        try
        {
            var usuario = User.Identity?.Name ?? "admin";
            await _service.AprobarGrupoAsync(grupoId, nombreCanonical, usuario);
            Mensaje = $"Grupo #{grupoId} aprobado exitosamente.";
        }
        catch (Exception ex)
        {
            Error = $"Error al aprobar: {ex.Message}";
        }

        await CargarDatosAsync();
        return Page();
    }

    /// <summary>
    /// Rechazar un grupo
    /// </summary>
    public async Task<IActionResult> OnPostRechazarAsync(int grupoId)
    {
        try
        {
            await _service.RechazarGrupoAsync(grupoId);
            Mensaje = $"Grupo #{grupoId} rechazado.";
        }
        catch (Exception ex)
        {
            Error = $"Error al rechazar: {ex.Message}";
        }

        await CargarDatosAsync();
        return Page();
    }

    /// <summary>
    /// Aprobar todos con alta confianza
    /// </summary>
    public async Task<IActionResult> OnPostAprobarTodosAsync(decimal umbral = 0.90m)
    {
        try
        {
            var usuario = User.Identity?.Name ?? "admin";
            await _service.AprobarTodosConConfianzaAsync(umbral, usuario);
            Mensaje = $"Todos los grupos con confianza >= {umbral:P0} fueron aprobados.";
        }
        catch (Exception ex)
        {
            Error = $"Error: {ex.Message}";
        }

        await CargarDatosAsync();
        return Page();
    }

    private async Task CargarDatosAsync()
    {
        Stats = await _service.ObtenerEstadisticasAsync();

        if (Tab == "aprobados")
        {
            GruposAprobados = await _service.ObtenerGruposAprobadosAsync(Buscar);
        }
        else
        {
            var (grupos, total) = await _service.ObtenerGruposPendientesAsync(Pagina, PageSize, Buscar);
            GruposPendientes = grupos;
            TotalGrupos = total;
        }
    }
}
