using TBD.MetricsModule.Services.Interfaces;

namespace TBD.MetricsModule.OpenTelemetry.Services;

public class OpenTelemetryMetricsServiceFactory : IMetricsServiceFactory
{
    public IMetricsService CreateMetricsService(string moduleName)
    {
        return new OpenTelemetryMetricsService(moduleName);
    }
}
