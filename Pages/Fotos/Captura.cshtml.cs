using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Fotos;

[Authorize]
public class CapturaModel : PageModel
{
    /// <summary>IdUsuario del claim (mapeado por DiamondsClaimsTransformation).</summary>
    public int IdUsuario { get; set; }

    /// <summary>Nombre del usuario autenticado.</summary>
    public string NombreUsuario { get; set; } = "";

    public void OnGet()
    {
        IdUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid : 0;
        NombreUsuario = User.Identity?.Name ?? "Usuario";
    }
}
