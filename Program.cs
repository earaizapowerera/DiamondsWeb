using System.Globalization;
using DiamondsWeb.Models;
using DiamondsWeb.Services;
using PowerEra.UserPortal.Component.Extensions;

// Forzar cultura es-MX para que los montos muestren $ en vez de ¤
var culturaMx = new CultureInfo("es-MX");
CultureInfo.DefaultThreadCurrentCulture = culturaMx;
CultureInfo.DefaultThreadCurrentUICulture = culturaMx;

var builder = WebApplication.CreateBuilder(args);

// UserPortal Component (auth, menu, seguridad)
builder.Services.AddUserPortalComponent(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("UserPortalDb")!;
    options.DataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "..", "shared-keys");
    options.ApplicationName = "UserPortalShared";
    options.LoginPath = "/Security/Auth/Login";
    options.LogoutPath = "/Security/Auth/Logout";
    options.AccessDeniedPath = "/Security/Auth/AccessDenied";
    options.SessionExpirationMinutes = 60;
    options.SlidingExpiration = true;
    options.AutoInitializeDatabase = true;
    options.DefaultAdminPassword = "u38a8fk3j0!";
    options.SystemName = "Diamonds Web";
    options.LogoUrl = "";
});

// AML Config
var amlConfig = new AmlConfig();
builder.Configuration.GetSection("AmlConfig").Bind(amlConfig);
builder.Services.AddSingleton(amlConfig);

// Catalog Service (Grupos, Monedas, Defaults, etc.)
var diamondsConnStr = builder.Configuration.GetConnectionString("DiamondsDb")!;
builder.Services.AddScoped<CatalogService>(sp => new CatalogService(
    diamondsConnStr,
    sp.GetRequiredService<ILogger<CatalogService>>()));

// AML Service
builder.Services.AddScoped<AmlService>(sp => new AmlService(
    diamondsConnStr,
    sp.GetRequiredService<AmlConfig>(),
    sp.GetRequiredService<ILogger<AmlService>>()));

// Jerarquias Service (config etiquetas)
builder.Services.AddScoped<JerarquiasService>(sp => new JerarquiasService(
    diamondsConnStr,
    sp.GetRequiredService<ILogger<JerarquiasService>>()));

// SPPLD Config & Service
var sppldConfig = new SppldConfig();
builder.Configuration.GetSection("SppldConfig").Bind(sppldConfig);
builder.Services.AddSingleton(sppldConfig);
builder.Services.AddScoped<SppldXmlService>();

// Etiquetas Service
builder.Services.AddScoped<EtiquetaService>(sp => new EtiquetaService(
    diamondsConnStr,
    sp.GetRequiredService<ILogger<EtiquetaService>>()));

// Proveedor Service (Razones Sociales, asignaciones N:N)
builder.Services.AddScoped<ProveedorService>(sp => new ProveedorService(
    diamondsConnStr,
    sp.GetRequiredService<ILogger<ProveedorService>>()));

// Remision Service (Actualizacion de Remisiones)
builder.Services.AddScoped<RemisionService>(sp => new RemisionService(
    diamondsConnStr,
    sp.GetRequiredService<ILogger<RemisionService>>()));

// Consignacion Service (Cuentas de Consignacion)
builder.Services.AddScoped<ConsignacionService>(sp => new ConsignacionService(
    diamondsConnStr,
    sp.GetRequiredService<ILogger<ConsignacionService>>()));

// Transfer Service (Transferencias de Mercancia entre tiendas)
builder.Services.AddScoped<TransferService>(sp => new TransferService(
    diamondsConnStr,
    sp.GetRequiredService<ILogger<TransferService>>()));

// Inventario Fisico Service
builder.Services.AddScoped<InventarioFisicoService>(sp => new InventarioFisicoService(
    diamondsConnStr,
    sp.GetRequiredService<ILogger<InventarioFisicoService>>()));

// Punto de Venta Service
builder.Services.AddScoped<PuntoVentaService>(sp => new PuntoVentaService(
    diamondsConnStr,
    sp.GetRequiredService<ILogger<PuntoVentaService>>()));

// Compuestas Service (Piezas Compuestas master-detail)
builder.Services.AddScoped<CompuestaService>(sp => new CompuestaService(
    diamondsConnStr,
    sp.GetRequiredService<ILogger<CompuestaService>>()));

// Moneda Service (Catálogo de Monedas)
builder.Services.AddScoped<MonedaService>(sp => new MonedaService(
    diamondsConnStr,
    sp.GetRequiredService<ILogger<MonedaService>>()));

// Diamantes Service (Catálogo de Diamantes)
builder.Services.AddScoped<DiamantesService>(sp => new DiamantesService(
    diamondsConnStr,
    sp.GetRequiredService<ILogger<DiamantesService>>()));

// Pieza Service (Alta y consulta de piezas)
builder.Services.AddScoped<PiezaService>(sp => new PiezaService(
    diamondsConnStr,
    sp.GetRequiredService<ILogger<PiezaService>>()));

// Sales Service (Ventas, consultas de notas/bajas/devoluciones)
builder.Services.AddScoped<SalesService>(sp => new SalesService(
    diamondsConnStr,
    sp.GetRequiredService<ILogger<SalesService>>()));

// Notas Service (Consulta de Notas de Venta)
builder.Services.AddScoped<NotasService>(sp => new NotasService(
    diamondsConnStr,
    sp.GetRequiredService<ILogger<NotasService>>()));

// Bajas Service (Consulta de Bajas)
builder.Services.AddScoped<BajasService>(sp => new BajasService(
    diamondsConnStr,
    sp.GetRequiredService<ILogger<BajasService>>()));

// Devolucion Service
builder.Services.AddScoped<DevolucionService>(sp => new DevolucionService(
    diamondsConnStr,
    sp.GetRequiredService<ILogger<DevolucionService>>()));

// Devoluciones Service (Pantalla de Devoluciones)
builder.Services.AddScoped<DevolucionesService>(sp => new DevolucionesService(
    diamondsConnStr,
    sp.GetRequiredService<ILogger<DevolucionesService>>()));

// Grupos Service (Catálogo de Grupos)
builder.Services.AddScoped<GruposService>(sp => new GruposService(
    diamondsConnStr,
    sp.GetRequiredService<ILogger<GruposService>>()));

// Divisores Service (Configuración de Divisores)
builder.Services.AddScoped<DivisoresService>(sp => new DivisoresService(
    diamondsConnStr,
    sp.GetRequiredService<ILogger<DivisoresService>>()));

// Cambio Status Service (Cambio de Status de Piezas)
builder.Services.AddScoped<CambioStatusService>(sp => new CambioStatusService(
    diamondsConnStr,
    sp.GetRequiredService<ILogger<CambioStatusService>>()));

// Actualizacion Service (Actualización de Piezas)
builder.Services.AddScoped<ActualizacionService>(sp => new ActualizacionService(
    diamondsConnStr,
    sp.GetRequiredService<ILogger<ActualizacionService>>()));

// Actualizaciones Service (Actualización de Facturas)
builder.Services.AddScoped<ActualizacionesService>(sp => new ActualizacionesService(
    diamondsConnStr,
    sp.GetRequiredService<ILogger<ActualizacionesService>>()));

// Catalogo Repetidas Service
builder.Services.AddScoped<CatalogoRepetidasService>(sp => new CatalogoRepetidasService(
    diamondsConnStr,
    sp.GetRequiredService<ILogger<CatalogoRepetidasService>>()));

// Lotes Repetidas Service
builder.Services.AddScoped<LotesRepetidasService>(sp => new LotesRepetidasService(
    diamondsConnStr,
    sp.GetRequiredService<ILogger<LotesRepetidasService>>()));

// Inventory Service (Existencias, PreBajas, Faltantes, Compuestas, Sencillas)
builder.Services.AddScoped<InventoryService>(sp => new InventoryService(
    diamondsConnStr,
    sp.GetRequiredService<ILogger<InventoryService>>()));

// Opcion Pago Service (Catálogo Opciones de Pago)
builder.Services.AddScoped<OpcionPagoService>(sp => new OpcionPagoService(
    diamondsConnStr,
    sp.GetRequiredService<ILogger<OpcionPagoService>>()));

// Tipos Cambio Service (Tipos de Cambio)
builder.Services.AddScoped<TiposCambioService>(sp => new TiposCambioService(
    diamondsConnStr,
    sp.GetRequiredService<ILogger<TiposCambioService>>()));

var app = builder.Build();

// UserPortal middleware
app.UseUserPortal();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Sirve static files del RCL (bootstrap, fontawesome, etc.)
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.MapControllers();

// Redirigir raíz y rutas huérfanas de UserPortal compartido a la pantalla principal
app.MapGet("/", context =>
{
    context.Response.Redirect("/Inventario");
    return Task.CompletedTask;
});
app.MapGet("/becario", context =>
{
    context.Response.Redirect("/Inventario");
    return Task.CompletedTask;
});

// Endpoint de diagnóstico para verificar conexión a DB
app.MapGet("/api/test-db", async (AmlService aml) =>
{
    var result = await aml.TestConexionAsync();
    return Results.Ok(new { status = result });
});

// Initialize UserPortal database
await app.Services.InitializeUserPortalDatabase();

app.Run();
