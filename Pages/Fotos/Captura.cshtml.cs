using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Fotos;

[Authorize]
public class CapturaModel : PageModel
{
    private readonly FotoService _fotoSvc;

    public CapturaModel(FotoService fotoSvc) => _fotoSvc = fotoSvc;

    public List<PiezaFoto> FotosRecientes { get; set; } = new();

    private int GetUserId()
    {
        return int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid : 0;
    }

    public async Task OnGetAsync()
    {
        var userId = GetUserId();
        if (userId > 0)
            FotosRecientes = await _fotoSvc.ObtenerFotosRecientesAsync(userId, 20);
    }
}
