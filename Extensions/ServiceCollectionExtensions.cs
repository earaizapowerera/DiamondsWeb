using DiamondsWeb.Models;
using DiamondsWeb.Services;

namespace DiamondsWeb.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra todos los servicios de Diamonds que siguen el patrón (connectionString, ILogger).
    /// </summary>
    public static IServiceCollection AddDiamondsServices(this IServiceCollection services, string connectionString)
    {
        // Servicios con constructor estándar: (string connectionString, ILogger<T> logger)
        services.AddScoped(sp => new CatalogService(connectionString, sp.GetRequiredService<ILogger<CatalogService>>()));
        services.AddScoped(sp => new JerarquiasService(connectionString, sp.GetRequiredService<ILogger<JerarquiasService>>()));
        services.AddScoped(sp => new EtiquetaService(connectionString, sp.GetRequiredService<ILogger<EtiquetaService>>()));
        services.AddScoped(sp => new ProveedorService(connectionString, sp.GetRequiredService<ILogger<ProveedorService>>()));
        services.AddScoped(sp => new RemisionService(connectionString, sp.GetRequiredService<ILogger<RemisionService>>()));
        services.AddScoped(sp => new ConsignacionService(connectionString, sp.GetRequiredService<ILogger<ConsignacionService>>()));
        services.AddScoped(sp => new TransferService(connectionString, sp.GetRequiredService<ILogger<TransferService>>()));
        services.AddScoped(sp => new InventarioFisicoService(connectionString, sp.GetRequiredService<ILogger<InventarioFisicoService>>()));
        services.AddScoped(sp => new PeriodosInventarioService(connectionString, sp.GetRequiredService<ILogger<PeriodosInventarioService>>()));
        services.AddScoped(sp => new PuntoVentaService(connectionString, sp.GetRequiredService<ILogger<PuntoVentaService>>()));
        services.AddScoped(sp => new ApartadoService(connectionString, sp.GetRequiredService<ILogger<ApartadoService>>()));
        services.AddScoped(sp => new HomologacionService(connectionString, sp.GetRequiredService<ILogger<HomologacionService>>()));
        services.AddScoped(sp => new CompuestaService(connectionString, sp.GetRequiredService<ILogger<CompuestaService>>()));
        services.AddScoped(sp => new MonedaService(connectionString, sp.GetRequiredService<ILogger<MonedaService>>()));
        services.AddScoped(sp => new DiamantesService(connectionString, sp.GetRequiredService<ILogger<DiamantesService>>()));
        services.AddScoped(sp => new PiezaService(connectionString, sp.GetRequiredService<ILogger<PiezaService>>()));
        services.AddScoped(sp => new SalesService(connectionString, sp.GetRequiredService<ILogger<SalesService>>()));
        services.AddScoped(sp => new NotasService(connectionString, sp.GetRequiredService<ILogger<NotasService>>()));
        services.AddScoped(sp => new BajasService(connectionString, sp.GetRequiredService<ILogger<BajasService>>()));
        services.AddScoped(sp => new DevolucionService(connectionString, sp.GetRequiredService<ILogger<DevolucionService>>()));
        services.AddScoped(sp => new DevolucionesService(connectionString, sp.GetRequiredService<ILogger<DevolucionesService>>()));
        services.AddScoped(sp => new GruposService(connectionString, sp.GetRequiredService<ILogger<GruposService>>()));
        services.AddScoped(sp => new DivisoresService(connectionString, sp.GetRequiredService<ILogger<DivisoresService>>()));
        services.AddScoped(sp => new CambioStatusService(connectionString, sp.GetRequiredService<ILogger<CambioStatusService>>()));
        services.AddScoped(sp => new ActualizacionService(connectionString, sp.GetRequiredService<ILogger<ActualizacionService>>()));
        services.AddScoped(sp => new ActualizacionesService(connectionString, sp.GetRequiredService<ILogger<ActualizacionesService>>()));
        services.AddScoped(sp => new CatalogoRepetidasService(connectionString, sp.GetRequiredService<ILogger<CatalogoRepetidasService>>()));
        services.AddScoped(sp => new LotesRepetidasService(connectionString, sp.GetRequiredService<ILogger<LotesRepetidasService>>()));
        services.AddScoped(sp => new InventoryService(connectionString, sp.GetRequiredService<ILogger<InventoryService>>()));
        services.AddScoped(sp => new OpcionPagoService(connectionString, sp.GetRequiredService<ILogger<OpcionPagoService>>()));
        services.AddScoped(sp => new TiposCambioService(connectionString, sp.GetRequiredService<ILogger<TiposCambioService>>()));
        services.AddScoped(sp =>
        {
            var env = sp.GetRequiredService<IWebHostEnvironment>();
            var fotosPath = Path.Combine(env.WebRootPath, "fotos-piezas");
            return new FotoService(connectionString, fotosPath, sp.GetRequiredService<ILogger<FotoService>>());
        });

        // Servicios con constructor especial
        services.AddScoped(sp => new AmlService(
            connectionString,
            sp.GetRequiredService<AmlConfig>(),
            sp.GetRequiredService<ILogger<AmlService>>()));

        // Servicios sin dependencia de connectionString
        services.AddScoped<SppldXmlService>();

        return services;
    }
}
