using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Piezas;

[Authorize]
public class AltaModel : PageModel
{
    private readonly PiezaService _svc;

    public AltaModel(PiezaService svc) => _svc = svc;

    // Modo edicion
    [BindProperty(SupportsGet = true)] public string? Cb { get; set; }
    public bool EsEdicion => !string.IsNullOrEmpty(Cb);

    // Pieza
    [BindProperty] public Pieza Pieza { get; set; } = new();
    [BindProperty] public string? Observaciones { get; set; }
    [BindProperty] public int IdEtiqueta { get; set; } = 2;
    [BindProperty] public string TabCaracteristica { get; set; } = "Oro";
    [BindProperty] public string TabCosto { get; set; } = "Pieza";

    // Remision actual
    [BindProperty(SupportsGet = true)] public int? IdRemision { get; set; }
    public Remision? RemisionActual { get; set; }

    // Catalogos
    public List<ProveedorInfo> Proveedores { get; set; } = new();
    public List<GrupoPieza> Grupos { get; set; } = new();
    public List<Moneda> Monedas { get; set; } = new();
    public List<DivisorVenta> Divisores { get; set; } = new();
    public List<EtiquetaPlantilla> Etiquetas { get; set; } = new();

    public string? MensajeExito { get; set; }
    public string? MensajeError { get; set; }

    private async Task CargarCatalogosAsync()
    {
        Proveedores = await _svc.ObtenerProveedoresAsync();
        Grupos = await _svc.ObtenerGruposAsync();
        Monedas = await _svc.ObtenerMonedasAsync();
        Divisores = await _svc.ObtenerDivisoresAsync();
        Etiquetas = await _svc.ObtenerEtiquetasAsync();
    }

    public async Task OnGetAsync()
    {
        await CargarCatalogosAsync();
        MensajeExito = TempData["MensajeExito"] as string;

        if (EsEdicion)
        {
            var pieza = await _svc.ObtenerPiezaAsync(Cb!);
            if (pieza != null)
            {
                Pieza = pieza;
                Observaciones = pieza.Observaciones;
                IdRemision = pieza.IdRemision;

                // Determinar tab de caracteristica activa
                if (!string.IsNullOrEmpty(pieza.NumSerie))
                    TabCaracteristica = "Reloj";
                else if (pieza.Quilates > 0 || !string.IsNullOrEmpty(pieza.Pureza))
                    TabCaracteristica = "Diamante";
                else
                    TabCaracteristica = "Oro";

                // Determinar tab de costo activo
                if (pieza.Peso > 0)
                    TabCosto = "Peso";
                else
                    TabCosto = "Pieza";
            }
        }

        if (IdRemision.HasValue)
        {
            RemisionActual = await _svc.ObtenerRemisionAsync(IdRemision.Value);
            if (!EsEdicion)
            {
                Pieza.IdRemision = IdRemision.Value;
                // Cargar defaults del proveedor
                if (RemisionActual != null)
                    await AplicarDefaultsProveedorAsync(RemisionActual.Proveedor);
            }
        }
    }

    public async Task<IActionResult> OnPostGuardarAsync()
    {
        await CargarCatalogosAsync();

        // Recalcular server-side por seguridad
        RecalcularPrecios();

        Pieza.Observaciones = Observaciones;
        Pieza.IdTienda ??= 1;
        Pieza.IdLocalizacion ??= 1;
        Pieza.IdUsuario = 1; // TODO: obtener del claim de auth

        GuardarPiezaResult result;
        if (EsEdicion)
        {
            Pieza.CodigoBarras = Cb!;
            result = await _svc.ActualizarPiezaAsync(Pieza);
        }
        else
        {
            result = await _svc.CrearPiezaAsync(Pieza, IdEtiqueta);
        }

        if (result.Success)
        {
            TempData["MensajeExito"] = EsEdicion
                ? $"Pieza {result.CodigoBarras} actualizada"
                : $"Pieza {result.CodigoBarras} creada exitosamente";

            // Si tiene remision, regresar al alta para continuar capturando
            if (IdRemision.HasValue && !EsEdicion)
                return RedirectToPage("Alta", new { IdRemision });
            return RedirectToPage("Alta", new { cb = result.CodigoBarras });
        }

        MensajeError = result.Error;
        return Page();
    }

    private void RecalcularPrecios()
    {
        // Costos netos por seccion
        Pieza.CNPieza = Pieza.CBPieza * (1 - Pieza.DescPieza / 100m);
        Pieza.CBPeso = Pieza.Peso * Pieza.PrecioGramo;
        Pieza.CNPeso = Pieza.CBPeso * (1 - Pieza.DescPeso / 100m);
        Pieza.CNManoObra = Pieza.CBManoObra * (1 - Pieza.DescManoObra / 100m);

        // Totales
        Pieza.CBTotal = Pieza.CBPieza + Pieza.CBPeso + Pieza.CBManoObra;
        Pieza.CNTotal = Pieza.CNPieza + Pieza.CNPeso + Pieza.CNManoObra;

        // Factura
        if (Pieza.TCCosto > 0 && Pieza.IdFactura.HasValue)
        {
            Pieza.CBFactura = (Pieza.CBPieza + Pieza.CBPeso) * Pieza.TCCosto;
            Pieza.CNFactura = (Pieza.CNPieza + Pieza.CNPeso) * Pieza.TCCosto;
            if (Pieza.CBFactura > 0)
                Pieza.DescFactura = (1 - Pieza.CNFactura / Pieza.CBFactura) * 100m;
        }
        else
        {
            Pieza.CNFactura = Pieza.CBFactura * (1 - Pieza.DescFactura / 100m);
        }

        // Precio final
        var utilidad = Pieza.Utilidad > 0 ? Pieza.Utilidad : 1m;
        var utilidadExtra = Pieza.UtilidadExtra > 0 ? Pieza.UtilidadExtra : 1m;
        var impuesto = Pieza.Impuesto > 0 ? Pieza.Impuesto : 1m;
        var divisor = Pieza.Divisor > 0 ? Pieza.Divisor : 1m;
        var tcCot = Pieza.TCCotizacion > 0 ? Pieza.TCCotizacion : 1m;

        var precioDecimal = Pieza.CNTotal * utilidad * utilidadExtra * impuesto / divisor * tcCot;
        Pieza.Precio = (int)Math.Round(precioDecimal, 0);
    }

    private async Task AplicarDefaultsProveedorAsync(int proveedorId)
    {
        var prov = await _svc.ObtenerProveedorAsync(proveedorId);
        if (prov == null) return;

        // Utilidad
        if (decimal.TryParse(prov.DefaultUtilidad, out var util))
            Pieza.Utilidad = util;

        // Moneda y tipo de cambio
        if (prov.UtilizarMoneda && prov.IdMoneda.HasValue)
        {
            Pieza.IdMoneda = prov.IdMoneda.Value;
            var tc = await _svc.ObtenerTipoCambioAsync(prov.IdMoneda.Value);
            if (tc != null)
            {
                Pieza.TCCotizacion = tc.TipoCambioCotizacion;
            }
        }
        else
        {
            Pieza.IdMoneda = 1;
            Pieza.TCCotizacion = 1;
        }

        // Divisor
        var divisores = await _svc.ObtenerDivisoresAsync();
        var div = divisores.FirstOrDefault(d => d.IdDivisor == prov.IdDivisor);
        if (div != null)
        {
            Pieza.Divisor = div.Divisor;
            Pieza.IdDivisor = div.IdDivisor;
        }

        // Impuesto default (IVA 16%)
        Pieza.Impuesto = 1.16m;
        Pieza.UtilidadExtra = 1;

        // Etiqueta y caracteristica
        IdEtiqueta = prov.IdTabla;
        TabCaracteristica = prov.CaracteristicaDefault;
        TabCosto = prov.CostoDefault;
    }

    // ==================== API ENDPOINTS (JSON) ====================

    public async Task<IActionResult> OnGetProveedorAsync(int id)
    {
        var prov = await _svc.ObtenerProveedorAsync(id);
        if (prov == null) return new JsonResult(new { error = "No encontrado" });
        TipoCambio? tc = null;
        if (prov.UtilizarMoneda && prov.IdMoneda.HasValue)
            tc = await _svc.ObtenerTipoCambioAsync(prov.IdMoneda.Value);
        return new JsonResult(new { prov, tc });
    }

    public async Task<IActionResult> OnGetTipoCambioAsync(int idMoneda)
    {
        var tc = await _svc.ObtenerTipoCambioAsync(idMoneda);
        return new JsonResult(tc ?? new TipoCambio { IdMoneda = idMoneda, TipoCambioCotizacion = 1, TipoCambioVenta = 1 });
    }

    public async Task<IActionResult> OnGetUtilidadExtraAsync(decimal precioGramo, decimal tcCotizacion)
    {
        var ue = await _svc.CalcularUtilidadExtraAsync(precioGramo, tcCotizacion);
        return new JsonResult(new { utilidadExtra = ue });
    }

    public async Task<IActionResult> OnGetBuscarProveedoresAsync(string texto)
    {
        var provs = await _svc.BuscarProveedoresAsync(texto);
        return new JsonResult(provs.Select(p => new { p.Proveedor, p.NombreProveedor }));
    }

    public async Task<IActionResult> OnGetRazonesSocialesAsync(int proveedor)
    {
        var rs = await _svc.ObtenerRazonesSocialesAsync(proveedor);
        return new JsonResult(rs);
    }
}
