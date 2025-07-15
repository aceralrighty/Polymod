using TBD.MetricsModule.ModuleHealthCheck.Interfaces;
using TBD.MetricsModule.ModuleHealthCheck.ModuleChecks;

namespace TBD.MetricsModule.ModuleHealthCheck;

public static class HealthCheckModule
{
    public static IServiceCollection AddHealthModuleChecks(this IServiceCollection services)
    {
        services.AddScoped<IModuleHealthCheck, AuthModuleHealthCheck>();
        services.AddScoped<IModuleHealthCheck, StockPredictionModuleHealthCheck>();
        services.AddScoped<IModuleHealthCheck, UserModuleHealthCheck>();
        services.AddScoped<IModuleHealthCheck, RecommendationsModuleHealthCheck>();
        services.AddScoped<IModuleHealthCheck, AddressModuleHealthCheck>();
        services.AddScoped<IModuleHealthCheck, ScheduleModuleHealthCheck>();
        services.AddScoped<IModuleHealthCheck, ServiceModuleHealthCheck>();
        services.AddScoped<IModuleHealthCheck, MetricsModuleHealthCheck>();

        return services;
    }
}
