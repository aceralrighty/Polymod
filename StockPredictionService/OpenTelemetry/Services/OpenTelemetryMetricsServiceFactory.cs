using StockPredictionService.Services.Interfaces;

namespace StockPredictionService.OpenTelemetry.Services;

public class OpenTelemetryMetricsServiceFactory : IMetricsServiceFactory
{
    public IMetricsService CreateMetricsService(string moduleName)
    {
        return new OpenTelemetryMetricsService(moduleName);
    }
}
