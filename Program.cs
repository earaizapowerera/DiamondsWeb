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

// Lotes Repetidas Service (Alta masiva de piezas estándar)
builder.Services.AddScoped<LotesRepetidasService>(sp => new LotesRepetidasService(
    diamondsConnStr,
    sp.GetRequiredService<ILogger<LotesRepetidasService>>()));

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
