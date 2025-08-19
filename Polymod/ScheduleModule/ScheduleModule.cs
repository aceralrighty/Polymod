using Microsoft.EntityFrameworkCore;
using PolyMod.ScheduleModule.Data;
using PolyMod.ScheduleModule.Models;
using PolyMod.ScheduleModule.Repositories;
using PolyMod.ScheduleModule.Services;
using PolyMod.Shared.CachingConfiguration;
using PolyMod.Shared.Repositories;
using PolyMod.MetricsModule.OpenTelemetry;
using PolyMod.MetricsModule.Services;
using PolyMod.MetricsModule.Services.Interfaces;

namespace PolyMod.ScheduleModule;

public static class ScheduleModule
{
    public static IServiceCollection AddScheduleModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContextPool<ScheduleDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("ScheduleDb"), b => b.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null
            )));
        services.Configure<CacheOptions>("Schedule", options =>
        {
            options.DefaultCacheDuration = TimeSpan.FromMinutes(10);
            options.GetByIdCacheDuration = TimeSpan.FromMinutes(15);
            options.GetAllCacheDuration = TimeSpan.FromMinutes(5);
            options.EnableCaching = true;
            options.CacheKeyPrefix = "Schedule";
        });
        services.AddScoped<IScheduleRepository, ScheduleRepository>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.RegisterModuleForMetrics("ScheduleModule");
        services.AddScoped<IGenericRepository<Schedule>>(sp =>
            new GenericRepository<Schedule>(sp.GetRequiredService<ScheduleDbContext>()));
        services.Decorate<IGenericRepository<Schedule>, CachingRepositoryDecorator<Schedule>>();
        return services;
    }
}
