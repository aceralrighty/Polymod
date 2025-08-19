using PolyMod.MetricsModule.Services.Interfaces;

namespace PolyMod.MetricsModule.OpenTelemetry.Services;

public class OpenTelemetryMetricsServiceFactory : IMetricsServiceFactory
{
    public IMetricsService CreateMetricsService(string moduleName)
    {
        return new OpenTelemetryMetricsService(moduleName);
    }
}
