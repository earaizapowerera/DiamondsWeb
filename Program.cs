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

// SPPLD Config & Service
var sppldConfig = new SppldConfig();
builder.Configuration.GetSection("SppldConfig").Bind(sppldConfig);
builder.Services.AddSingleton(sppldConfig);
builder.Services.AddScoped<SppldXmlService>();

// Diamonds DB connection string (compartido por todos los servicios)
var diamondsConnStr = builder.Configuration.GetConnectionString("DiamondsDb")!;

// Helper: registra un servicio con (connectionString, ILogger<T>)
void AddDiamondsService<T>(IServiceCollection svc) where T : class =>
    svc.AddScoped<T>(sp => (T)Activator.CreateInstance(typeof(T),
        diamondsConnStr, sp.GetRequiredService<ILogger<T>>())!);

// AML Service (constructor especial: connectionString + AmlConfig + ILogger)
builder.Services.AddScoped<AmlService>(sp => new AmlService(
    diamondsConnStr,
    sp.GetRequiredService<AmlConfig>(),
    sp.GetRequiredService<ILogger<AmlService>>()));

// Servicios de dominio Diamonds
AddDiamondsService<ActualizacionService>(builder.Services);
AddDiamondsService<ActualizacionesService>(builder.Services);
AddDiamondsService<BajasService>(builder.Services);
AddDiamondsService<CambioStatusService>(builder.Services);
AddDiamondsService<CatalogService>(builder.Services);
AddDiamondsService<CatalogoRepetidasService>(builder.Services);
AddDiamondsService<CompuestaService>(builder.Services);
AddDiamondsService<ConsignacionService>(builder.Services);
AddDiamondsService<DevolucionService>(builder.Services);
AddDiamondsService<DevolucionesService>(builder.Services);
AddDiamondsService<DiamantesService>(builder.Services);
AddDiamondsService<DivisoresService>(builder.Services);
AddDiamondsService<EtiquetaService>(builder.Services);
AddDiamondsService<GruposService>(builder.Services);
AddDiamondsService<InventarioFisicoService>(builder.Services);
AddDiamondsService<InventoryService>(builder.Services);
AddDiamondsService<JerarquiasService>(builder.Services);
AddDiamondsService<LotesRepetidasService>(builder.Services);
AddDiamondsService<MonedaService>(builder.Services);
AddDiamondsService<NotasService>(builder.Services);
AddDiamondsService<OpcionPagoService>(builder.Services);
AddDiamondsService<PiezaService>(builder.Services);
AddDiamondsService<ProveedorService>(builder.Services);
AddDiamondsService<PuntoVentaService>(builder.Services);
AddDiamondsService<RemisionService>(builder.Services);
AddDiamondsService<SalesService>(builder.Services);
AddDiamondsService<TiposCambioService>(builder.Services);
AddDiamondsService<TransferService>(builder.Services);

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
