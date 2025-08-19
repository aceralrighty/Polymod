using PolyMod.StockPredictionService.Services.Interfaces;

namespace PolyMod.StockPredictionService.OpenTelemetry.Services;

public class OpenTelemetryMetricsServiceFactory : IMetricsServiceFactory
{
    public IMetricsService CreateMetricsService(string moduleName)
    {
        return new OpenTelemetryMetricsService(moduleName);
    }
}