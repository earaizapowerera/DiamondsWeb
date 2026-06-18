using DiamondsWeb.Extensions;
using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Inventario.Transferencias;

[Authorize]
public class IndexModel : PageModel
{
    private readonly TransferService _transferService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(TransferService transferService, ILogger<IndexModel> logger)
    {
        _transferService = transferService;
        _logger = logger;
    }

    public List<Tienda> Tiendas { get; set; } = new();
    public List<PiezaEnTransito> PiezasEnTransito { get; set; } = new();
    public List<LoteEnTransito> RepetidasEnTransito { get; set; } = new();
    public List<LogTransferencia> LogReciente { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? IdTienda { get; set; }

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        SuccessMessage = TempData["Success"]?.ToString();
        ErrorMessage = TempData["Error"]?.ToString();
        await CargarDatosAsync();
    }

    /// <summary>
    /// Recibir pieza individual (sencilla o compuesta)
    /// </summary>
    public async Task<IActionResult> OnPostRecibirAsync(string codigoBarras, int idTienda)
    {
        IdTienda = idTienda;
        if (string.IsNullOrWhiteSpace(codigoBarras))
        {
            TempData["Error"] = "Ingrese un código de barras.";
            return RedirectToPage(new { IdTienda = idTienda });
        }

        var result = await _transferService.RecibirPiezaAsync(codigoBarras.Trim(), idTienda, GetUsuarioId());
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToPage(new { IdTienda = idTienda });
    }

    /// <summary>
    /// Enviar pieza individual a otra tienda
    /// </summary>
    public async Task<IActionResult> OnPostEnviarAsync(string codigoBarras, int idTienda, int idTiendaDestino)
    {
        IdTienda = idTienda;
        if (string.IsNullOrWhiteSpace(codigoBarras))
        {
            TempData["Error"] = "Ingrese un código de barras.";
            return RedirectToPage(new { IdTienda = idTienda });
        }

        var result = await _transferService.EnviarPiezaAsync(
            codigoBarras.Trim(), idTienda, idTiendaDestino, GetUsuarioId());
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToPage(new { IdTienda = idTienda });
    }

    /// <summary>
    /// Enviar piezas repetidas por cantidad
    /// </summary>
    public async Task<IActionResult> OnPostEnviarRepetidasAsync(
        string codigoBarras, int cantidad, int idTienda, int idTiendaDestino)
    {
        IdTienda = idTienda;
        if (string.IsNullOrWhiteSpace(codigoBarras) || cantidad <= 0)
        {
            TempData["Error"] = "Ingrese un código de barras válido y cantidad mayor a 0.";
            return RedirectToPage(new { IdTienda = idTienda });
        }

        var result = await _transferService.EnviarRepetidasAsync(
            codigoBarras.Trim(), cantidad, idTienda, idTiendaDestino, GetUsuarioId());
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToPage(new { IdTienda = idTienda });
    }

    /// <summary>
    /// Recibir un lote de piezas repetidas (valida cantidad exacta como el VB6)
    /// </summary>
    public async Task<IActionResult> OnPostRecibirRepetidasAsync(int idLote, int cantidad, int idTienda)
    {
        IdTienda = idTienda;
        var result = await _transferService.RecibirRepetidasAsync(idLote, cantidad, idTienda, GetUsuarioId());
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToPage(new { IdTienda = idTienda });
    }

    private async Task CargarDatosAsync()
    {
        Tiendas = await _transferService.ObtenerTiendasAsync();

        // Default: tienda del usuario (claim IdTienda) o primera tienda
        if (!IdTienda.HasValue)
        {
            var tiendaClaim = User.FindFirst("IdTienda")?.Value;
            IdTienda = int.TryParse(tiendaClaim, out var tid) ? tid : Tiendas.FirstOrDefault()?.IdTienda;
        }

        if (IdTienda.HasValue)
        {
            PiezasEnTransito = await _transferService.ObtenerPiezasEnTransitoAsync(IdTienda.Value);
            RepetidasEnTransito = await _transferService.ObtenerRepetidasEnTransitoAsync(IdTienda.Value);
        }

        LogReciente = await _transferService.ObtenerLogRecienteAsync(30);
    }

    private int GetUsuarioId()
    {
        return User.GetRequiredIdUsuario();
    }
}
