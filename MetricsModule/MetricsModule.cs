using TBD.MetricsModule.Services;
using TBD.MetricsModule.Services.Interfaces;

namespace TBD.MetricsModule;

public static class MetricsModule
{
    public static IServiceCollection AddMetricsModule(this IServiceCollection services)
    {
        // services.AddSingleton<IMetricsServiceFactory, MetricsServiceFactory>();
        return services;
    }
}
