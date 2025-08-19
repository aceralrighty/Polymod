namespace PolyMod.MetricsModule.Services.Interfaces;

public interface IMetricsServiceFactory
{
    IMetricsService CreateMetricsService(string moduleName);
}
