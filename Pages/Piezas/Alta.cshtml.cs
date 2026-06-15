using DiamondsWeb.Models;
using DiamondsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DiamondsWeb.Pages.Piezas;

[Authorize]
[RequestSizeLimit(10 * 1024 * 1024)] // 10 MB para upload de fotos
public class AltaModel : PageModel
{
    private readonly PiezaService _svc;
    private readonly FotoService _fotoSvc;

    public AltaModel(PiezaService svc, FotoService fotoSvc)
    {
        _svc = svc;
        _fotoSvc = fotoSvc;
    }

    // Modo edicion
    [BindProperty(SupportsGet = true)] public string? Cb { get; set; }
    public bool EsEdicion => !string.IsNullOrEmpty(Cb);

    // Pieza
    [BindProperty] public Pieza Pieza { get; set; } = new();
    [BindProperty] public string? Observaciones { get; set; }
    [BindProperty] public int IdEtiqueta { get; set; } = 2;
    [BindProperty] public string TabCaracteristica { get; set; } = "Oro";
    [BindProperty] public string TabCosto { get; set; } = "Pieza";

    // Remision y Factura
    [BindProperty(SupportsGet = true)] public int? IdRemision { get; set; }
    [BindProperty(SupportsGet = true)] public int? IdFactura { get; set; }
    public Remision? RemisionActual { get; set; }
    public Factura? FacturaActual { get; set; }

    // Grid de piezas de la remision/factura actual
    public List<PiezaResumen> PiezasRemision { get; set; } = new();
    public RemisionTotales? Totales { get; set; }

    // Foto
    public List<PiezaFoto> FotosRecientes { get; set; } = new();
    public PiezaFoto? FotoActual { get; set; }

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
            // Cargar grid de piezas de la remision
            PiezasRemision = await _svc.ObtenerPiezasPorRemisionAsync(IdRemision.Value);
            Totales = await _svc.ObtenerTotalesRemisionAsync(IdRemision.Value);
        }

        // Cargar factura (desde query string o desde la pieza en edicion)
        if (IdFactura.HasValue)
        {
            FacturaActual = await _svc.ObtenerFacturaAsync(IdFactura.Value);
            if (!EsEdicion)
                Pieza.IdFactura = IdFactura.Value;
        }
        else if (EsEdicion && Pieza.IdFactura.HasValue)
        {
            FacturaActual = await _svc.ObtenerFacturaAsync(Pieza.IdFactura.Value);
            IdFactura = Pieza.IdFactura;
        }

        // Cargar foto actual (si edicion y tiene ArchivoFoto)
        if (EsEdicion && !string.IsNullOrEmpty(Pieza.ArchivoFoto))
            FotoActual = await _fotoSvc.ObtenerFotoPorNombreAsync(Pieza.ArchivoFoto);

        // Cargar ultimas 3 fotos moviles no vinculadas
        var fotoUid = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var fid) ? fid : 1;
        FotosRecientes = await _fotoSvc.ObtenerFotosRecientesAsync(fotoUid, 3, "mobile");
    }

    public async Task<IActionResult> OnPostGuardarAsync()
    {
        await CargarCatalogosAsync();

        // Recalcular server-side por seguridad
        RecalcularPrecios();

        var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid
            : throw new UnauthorizedAccessException("IdUsuario claim not found");
        var idTienda = int.TryParse(User.FindFirst("IdTienda")?.Value, out var tid) ? tid : 1;
        Pieza.Observaciones = Observaciones;
        Pieza.IdTienda ??= idTienda;
        Pieza.IdLocalizacion ??= idTienda;
        Pieza.IdUsuario = idUsuario;

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
            // Vincular foto si se selecciono una
            if (!string.IsNullOrEmpty(Pieza.ArchivoFoto))
                await _fotoSvc.VincularFotoPorNombreAsync(Pieza.ArchivoFoto, result.CodigoBarras!);

            TempData["MensajeExito"] = EsEdicion
                ? $"Pieza {result.CodigoBarras} actualizada"
                : $"Pieza {result.CodigoBarras} creada exitosamente";

            // Si tiene remision, regresar al alta para continuar capturando
            if (IdRemision.HasValue && !EsEdicion)
                return RedirectToPage("Alta", new { IdRemision, IdFactura });
            return RedirectToPage("Alta", new { cb = result.CodigoBarras, IdRemision, IdFactura });
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
        var utilidad = (Pieza.Utilidad ?? 0m) > 0 ? (Pieza.Utilidad ?? 1m) : 1m;
        var utilidadExtra = (Pieza.UtilidadExtra ?? 0m) > 0 ? (Pieza.UtilidadExtra ?? 1m) : 1m;
        var impuesto = (Pieza.Impuesto ?? 0m) > 0 ? (Pieza.Impuesto ?? 1m) : 1m;
        var divisor = (Pieza.Divisor ?? 0m) > 0 ? (Pieza.Divisor ?? 1m) : 1m;
        var tcCot = (Pieza.TCCotizacion ?? 0m) > 0 ? (Pieza.TCCotizacion ?? 1m) : 1m;

        var precioDecimal = (Pieza.CNTotal ?? 0m) * utilidad * utilidadExtra * impuesto / divisor * tcCot;
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

    // ==================== FOTOS ====================

    /// <summary>Sube una foto desde el navegador web via AJAX.</summary>
    public async Task<IActionResult> OnPostSubirFotoAsync(IFormFile foto)
    {
        if (foto == null || foto.Length == 0)
            return new JsonResult(new { success = false, error = "No se envio archivo" });

        using var stream = foto.OpenReadStream();
        var fotoUserId = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var fu) ? fu
            : throw new UnauthorizedAccessException("IdUsuario claim not found");
        var result = await _fotoSvc.SubirFotoAsync(
            stream, foto.FileName, foto.ContentType, foto.Length, fotoUserId, "web");

        return new JsonResult(new
        {
            success = result.Success,
            url = result.Url,
            storedFileName = result.StoredFileName,
            fotoId = result.FotoId,
            error = result.Error
        });
    }

    /// <summary>Lista ultimas N fotos moviles no vinculadas.</summary>
    public async Task<IActionResult> OnGetFotosRecientesAsync(int count = 3)
    {
        var recUserId = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var ru) ? ru
            : throw new UnauthorizedAccessException("IdUsuario claim not found");
        var fotos = await _fotoSvc.ObtenerFotosRecientesAsync(recUserId, count, "mobile");
        return new JsonResult(fotos.Select(f => new
        {
            f.Id, f.Url, f.FileName, f.Source,
            uploadedAt = f.UploadedAt.ToString("yyyy-MM-dd HH:mm")
        }));
    }

    /// <summary>Elimina una foto.</summary>
    public async Task<IActionResult> OnPostEliminarFotoAsync(int fotoId)
    {
        var ok = await _fotoSvc.EliminarFotoAsync(fotoId);
        return new JsonResult(new { success = ok });
    }

    // ==================== CREAR REMISION/FACTURA AL VUELO ====================

    public async Task<IActionResult> OnPostCrearRemisionAsync(int proveedor, string? numeroRemision,
        DateTime? fechaRemision, bool consignacion)
    {
        try
        {
            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid : 1;
            var idTienda = int.TryParse(User.FindFirst("IdTienda")?.Value, out var tid) ? tid : 1;
            var remision = new Remision
            {
                Proveedor = proveedor,
                NumeroRemision = string.IsNullOrWhiteSpace(numeroRemision) ? "S/N" : numeroRemision,
                FechaRemision = fechaRemision ?? DateTime.UtcNow,
                Consignacion = consignacion,
                IdUsuario = idUsuario,
                IdTienda = idTienda,
                IdLocalizacion = idTienda
            };
            var id = await _svc.CrearRemisionAsync(remision);
            return new JsonResult(new { success = true, idRemision = id });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostCrearFacturaAsync(int proveedor, string? folioFactura,
        int idRazonSocial, DateTime? fechaFactura)
    {
        try
        {
            var idUsuario = int.TryParse(User.FindFirst("IdUsuario")?.Value, out var uid) ? uid : 1;
            var factura = new Factura
            {
                FolioFactura = string.IsNullOrWhiteSpace(folioFactura) ? "S/N" : folioFactura,
                Proveedor = proveedor,
                IdRazonSocialProveedor = idRazonSocial,
                FechaFactura = fechaFactura ?? DateTime.UtcNow,
                IdUsuario = idUsuario
            };
            var id = await _svc.CrearFacturaAsync(factura);
            return new JsonResult(new { success = true, idFactura = id });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }

    public Task<IActionResult> OnPostVincularFacturaAsync(int idRemision, int idFactura)
    {
        // La factura se vincula por pieza individual (campo IdFactura en Piezas)
        // Este handler solo confirma la operacion - el link real ocurre al guardar la pieza
        return Task.FromResult<IActionResult>(new JsonResult(new { success = true }));
    }

    // ==================== BUSQUEDA DE REMISIONES/FACTURAS ====================

    public async Task<IActionResult> OnGetBuscarRemisionesAsync(string? texto)
    {
        var remisiones = await _svc.ObtenerRemisionesAsync();
        if (!string.IsNullOrWhiteSpace(texto))
        {
            var t = texto.ToLower();
            remisiones = remisiones
                .Where(r => (r.NombreProveedor ?? "").ToLower().Contains(t)
                         || (r.NumeroRemision ?? "").ToLower().Contains(t)
                         || r.IdRemision.ToString().Contains(t))
                .ToList();
        }
        return new JsonResult(remisiones.Select(r => new
        {
            r.IdRemision,
            r.NombreProveedor,
            r.NumeroRemision,
            fechaRemision = r.FechaRemision?.ToString("dd/MM/yy"),
            r.Consignacion,
            r.CantidadPiezas
        }));
    }

    public async Task<IActionResult> OnGetBuscarFacturasAsync(string? texto)
    {
        var facturas = await _svc.ObtenerFacturasAsync(texto);
        return new JsonResult(facturas.Select(f => new
        {
            f.IdFactura,
            f.FolioFactura,
            f.NombreProveedor,
            fechaFactura = f.FechaFactura?.ToString("dd/MM/yy"),
            totalBruto = f.TotalBruto?.ToString("N2"),
            totalNeto = f.TotalNeto?.ToString("N2")
        }));
    }

    public async Task<IActionResult> OnGetPiezasRemisionAsync(int idRemision)
    {
        var piezas = await _svc.ObtenerPiezasPorRemisionAsync(idRemision);
        var totales = await _svc.ObtenerTotalesRemisionAsync(idRemision);
        return new JsonResult(new { piezas, totales });
    }
}
