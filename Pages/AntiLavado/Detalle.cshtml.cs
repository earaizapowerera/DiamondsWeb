using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.AntiLavado;

[Authorize]
[RequestSizeLimit(10 * 1024 * 1024)]
public class DetalleModel : PageModel
{
    private readonly AmlService _amlService;
    private readonly FotoService _fotoService;
    private readonly AmlConfig _config;
    private readonly IWebHostEnvironment _env;

    public DetalleModel(AmlService amlService, FotoService fotoService, AmlConfig config, IWebHostEnvironment env)
    {
        _amlService = amlService;
        _fotoService = fotoService;
        _config = config;
        _env = env;
    }

    public List<NotaDetalle> Notas { get; set; } = new();
    public Dictionary<int, List<PagoDetalle>> DesglosePageos { get; set; } = new();
    public string ClienteNombre { get; set; } = string.Empty;
    public decimal TotalAcumulado { get; set; }
    public string NivelAlerta { get; set; } = "Normal";
    public AmlConfig Config => _config;
    public DateTime PeriodoDesde { get; set; }
    public DateTime PeriodoHasta { get; set; }
    public List<AmlIdentificacion> Identificaciones { get; set; } = new();
    public List<PiezaFoto> FotosMovilRecientes { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Cliente { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Mes { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Anio { get; set; }

    private int GetUserId()
    {
        var claim = User.FindFirst("IdUsuario")?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrEmpty(Cliente))
            return RedirectToPage("Index");

        Mes ??= DateTime.UtcNow.Month;
        Anio ??= DateTime.UtcNow.Year;

        PeriodoHasta = new DateTime(Anio.Value, Mes.Value, DateTime.DaysInMonth(Anio.Value, Mes.Value));
        PeriodoDesde = PeriodoHasta.AddMonths(-5);
        PeriodoDesde = new DateTime(PeriodoDesde.Year, PeriodoDesde.Month, 1);

        ClienteNombre = Cliente;
        Notas = await _amlService.ObtenerNotasClienteAsync(Cliente, Mes.Value, Anio.Value);
        TotalAcumulado = Notas.Sum(n => n.Total);

        var idNotas = Notas.Select(n => n.IdNota).ToList();
        DesglosePageos = await _amlService.ObtenerDesglosePageosAsync(idNotas);

        if (TotalAcumulado >= _config.MontoAvisoSAT)
            NivelAlerta = "AvisoSAT";
        else if (TotalAcumulado >= _config.MontoIdentificacion)
            NivelAlerta = "Identificacion";

        Identificaciones = await _amlService.ObtenerIdentificacionesAsync(Cliente);

        var userId = GetUserId();
        if (userId > 0)
            FotosMovilRecientes = await _fotoService.ObtenerFotosRecientesAsync(userId, 20);

        return Page();
    }

    public async Task<IActionResult> OnPostUploadAsync(
        [FromForm] string cliente, [FromForm] string tipoDocumento,
        [FromForm] string? notas, [FromForm] IFormFile archivo,
        [FromQuery] int? mes, [FromQuery] int? anio)
    {
        if (archivo == null || archivo.Length == 0 || string.IsNullOrEmpty(cliente))
            return RedirectToPage(new { Cliente = cliente, Mes = mes, Anio = anio });

        var username = User.Identity?.Name ?? "unknown";
        using var stream = archivo.OpenReadStream();
        await _amlService.SubirIdentificacionAsync(
            stream, archivo.FileName, archivo.ContentType, archivo.Length,
            cliente, tipoDocumento, username, notas, _env.WebRootPath);

        return RedirectToPage(new { Cliente = cliente, Mes = mes, Anio = anio });
    }

    public async Task<IActionResult> OnPostLinkFotoAsync(
        [FromForm] string cliente, [FromForm] int fotoId, [FromForm] string tipoDocumento,
        [FromForm] string? notas, [FromQuery] int? mes, [FromQuery] int? anio)
    {
        if (fotoId <= 0 || string.IsNullOrEmpty(cliente))
            return RedirectToPage(new { Cliente = cliente, Mes = mes, Anio = anio });

        var username = User.Identity?.Name ?? "unknown";
        await _amlService.VincularFotoComoIdentificacionAsync(
            fotoId, cliente, tipoDocumento, username, notas, _env.WebRootPath);

        return RedirectToPage(new { Cliente = cliente, Mes = mes, Anio = anio });
    }

    public async Task<IActionResult> OnPostDeleteIdAsync(
        [FromForm] string cliente, [FromForm] int idDoc,
        [FromQuery] int? mes, [FromQuery] int? anio)
    {
        await _amlService.EliminarIdentificacionAsync(idDoc, _env.WebRootPath);
        return RedirectToPage(new { Cliente = cliente, Mes = mes, Anio = anio });
    }
}
