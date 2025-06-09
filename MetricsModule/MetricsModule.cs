using TBD.MetricsModule.Services;

namespace TBD.MetricsModule;

public static class MetricsModule
{
    public static IServiceCollection AddMetricsModule(this IServiceCollection services)
    {
        services.AddSingleton<IMetricsServiceFactory, MetricsServiceFactory>();
        return services;
    }
}
