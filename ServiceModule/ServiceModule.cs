using Microsoft.EntityFrameworkCore;
using TBD.MetricsModule.OpenTelemetry;
using TBD.MetricsModule.Services;
using TBD.MetricsModule.Services.Interfaces;
using TBD.ServiceModule.Data;
using TBD.ServiceModule.Models;
using TBD.ServiceModule.Repositories;
using TBD.ServiceModule.Services;
using TBD.Shared.CachingConfiguration;
using TBD.Shared.EntityMappers;
using TBD.Shared.Repositories;

namespace TBD.ServiceModule;

public static class ServiceModule
{
    public static IServiceCollection AddServiceModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ServiceDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("ServiceDb")));
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
