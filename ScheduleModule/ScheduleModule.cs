using Microsoft.EntityFrameworkCore;
using TBD.MetricsModule.Services;
using TBD.ScheduleModule.Data;
using TBD.ScheduleModule.Models;
using TBD.ScheduleModule.Repositories;
using TBD.ScheduleModule.Services;
using TBD.Shared.Repositories;

namespace TBD.ScheduleModule;

public static class ScheduleModule
{
    public static IServiceCollection AddScheduleModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ScheduleDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("ScheduleDb")));
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
        services.AddSingleton<IMetricsServiceFactory, MetricsServiceFactory>();
        services.AddScoped<IGenericRepository<Schedule>>(sp =>
            new GenericRepository<Schedule>(sp.GetRequiredService<ScheduleDbContext>()));
        services.Decorate<IGenericRepository<Schedule>, CachingRepositoryDecorator<Schedule>>();
        return services;
    }
}
