using Microsoft.EntityFrameworkCore;
using PolyMod.ServiceModule.Data;
using PolyMod.ServiceModule.Models;
using PolyMod.ServiceModule.Repositories;
using PolyMod.ServiceModule.Services;
using PolyMod.Shared.CachingConfiguration;
using PolyMod.Shared.EntityMappers;
using PolyMod.Shared.Repositories;
using PolyMod.MetricsModule.OpenTelemetry;
using PolyMod.MetricsModule.Services;
using PolyMod.MetricsModule.Services.Interfaces;

namespace PolyMod.ServiceModule;

public static class ServiceModule
{
    public static IServiceCollection AddServiceModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContextPool<ServiceDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("ServiceDb"), b => b.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null
            )));
        services.Configure<CacheOptions>("Service", options =>
        {
            options.DefaultCacheDuration = TimeSpan.FromMinutes(10);
            options.GetByIdCacheDuration = TimeSpan.FromMinutes(15);
            options.GetAllCacheDuration = TimeSpan.FromMinutes(5);
            options.EnableCaching = true;
            options.CacheKeyPrefix = "Service";
        });
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IServicesService, ServicesService>();
        services.RegisterModuleForMetrics("ServiceModule");
        services.AddScoped<IGenericRepository<Service>>(sp =>
            new GenericRepository<Service>(sp.GetRequiredService<ServiceDbContext>()));
        services.Decorate<IGenericRepository<Service>, CachingRepositoryDecorator<Service>>();
        services.AddAutoMapper(typeof(ServiceMapping).Assembly);
        return services;
    }
}
