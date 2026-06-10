using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Procesos.ActualizacionRemisiones;

[Authorize]
public class IndexModel : PageModel
{
    public void OnGet()
    {
    }
}
