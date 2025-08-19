namespace PolyMod.StockPredictionService.Services.Interfaces;

public interface IMetricsServiceFactory
{
    IMetricsService CreateMetricsService(string moduleName);
}
