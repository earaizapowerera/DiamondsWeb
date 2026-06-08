using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.AntiLavado;

[Authorize]
public class HistorialModel : PageModel
{
    private readonly AmlService _amlService;

    public HistorialModel(AmlService amlService) => _amlService = amlService;

    public List<ClienteReportado> Reportados { get; set; } = new();

    public async Task OnGetAsync()
    {
        Reportados = await _amlService.ObtenerHistorialReportadosAsync();
    }
}
