using PolyMod.StockPredictionService.Services.Interfaces;

namespace PolyMod.StockPredictionService.Services;

public class MetricsServiceFactory : IMetricsServiceFactory
{
    public IMetricsService CreateMetricsService(string moduleName)
    {
        return new MetricsService(moduleName);
    }
}
