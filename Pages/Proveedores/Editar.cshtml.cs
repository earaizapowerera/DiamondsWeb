using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Proveedores;

[Authorize]
public class EditarModel : PageModel
{
    private readonly ProveedorService _service;
    private readonly ILogger<EditarModel> _logger;

    public EditarModel(ProveedorService service, ILogger<EditarModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    [BindProperty]
    public ProveedorDetalle Prov { get; set; } = new();

    public bool EsNuevo => Prov.Proveedor == 0;
    public string? ErrorMessage { get; set; }

    // Catálogos para dropdowns
    public List<DefaultUtilidadItem> DefaultsUtilidad { get; set; } = new();
    public List<CatalogoItem> Monedas { get; set; } = new();
    public List<DivisorItem> Divisores { get; set; } = new();
    public List<CatalogoItem> TablasJerarquias { get; set; } = new();

    // Valores fijos para combos de Caracteristica y Costo
    public static readonly string[] CaracteristicaOpciones = { "Diamante", "Oro", "Reloj" };
    public static readonly string[] CostoOpciones = { "Pieza", "Peso" };

    public async Task OnGetAsync(int? id)
    {
        await CargarCatalogosAsync();

        if (id.HasValue)
        {
            var prov = await _service.ObtenerPorIdAsync(id.Value);
            if (prov == null)
            {
                ErrorMessage = $"No se encontro proveedor #{id}";
                return;
            }
            Prov = prov;
        }
        else
        {
            // Defaults para nuevo proveedor
            Prov.CaracteristicaDefault = "Oro";
            Prov.CostoDefault = "Pieza";
            Prov.IdMoneda = 1;
            Prov.IdDefaultUtilidad = DefaultsUtilidad.FirstOrDefault()?.IdDefaultUtilidad ?? 1;
            Prov.IdDivisor = Divisores.FirstOrDefault()?.IdDivisor ?? 1;
            Prov.IdTabla = TablasJerarquias.FirstOrDefault()?.Id ?? 2;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Quitar validaciones de campos de solo lectura
        ModelState.Remove("Prov.DefaultUtilidad");
        ModelState.Remove("Prov.Moneda");
        ModelState.Remove("Prov.DivisorDescripcion");
        ModelState.Remove("Prov.TablaDescripcion");

        if (!ModelState.IsValid)
        {
            await CargarCatalogosAsync();
            ErrorMessage = "Corrige los errores del formulario.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Prov.NombreProveedor))
        {
            await CargarCatalogosAsync();
            ErrorMessage = "El nombre del proveedor es obligatorio.";
            return Page();
        }

        try
        {
            if (Prov.Proveedor == 0)
            {
                var newId = await _service.CrearAsync(Prov);
                TempData["Success"] = $"Proveedor '{Prov.NombreProveedor}' creado con Id #{newId}.";
            }
            else
            {
                await _service.ActualizarAsync(Prov);
                TempData["Success"] = $"Proveedor '{Prov.NombreProveedor}' actualizado.";
            }

            return RedirectToPage("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error guardando proveedor {Id}", Prov.Proveedor);
            await CargarCatalogosAsync();
            ErrorMessage = $"Error al guardar: {ex.Message}";
            return Page();
        }
    }

    private async Task CargarCatalogosAsync()
    {
        DefaultsUtilidad = await _service.ObtenerDefaultsUtilidadAsync();
        Monedas = await _service.ObtenerMonedasAsync();
        Divisores = await _service.ObtenerDivisoresAsync();
        TablasJerarquias = await _service.ObtenerTablasJerarquiasAsync();
    }
}
