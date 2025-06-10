using Microsoft.EntityFrameworkCore;
using TBD.MetricsModule.Services;
using TBD.ScheduleModule.Data;
using TBD.ScheduleModule.Repositories;
using TBD.ScheduleModule.Services;

namespace TBD.ScheduleModule;

public static class ScheduleModule
{
    public static IServiceCollection AddScheduleModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ScheduleDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("ScheduleDb")));
        services.AddScoped<IScheduleRepository, ScheduleRepository>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddSingleton<IMetricsServiceFactory, MetricsServiceFactory>();
        return services;
    }
}
