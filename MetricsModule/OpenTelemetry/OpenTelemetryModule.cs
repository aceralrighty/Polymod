using TBD.MetricsModule.OpenTelemetry.Services;
using TBD.MetricsModule.Services.Interfaces;

namespace TBD.MetricsModule.OpenTelemetry;

public static class OpenTelemetryModule
{
    public static IServiceCollection AddOpenTelemetryMetricsModule(this IServiceCollection services)
    {
        services.AddSingleton<IMetricsServiceFactory, OpenTelemetryMetricsServiceFactory>();
        return services;
    }
}
