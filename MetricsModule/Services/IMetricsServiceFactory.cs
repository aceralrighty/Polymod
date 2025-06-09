namespace TBD.MetricsModule.Services;

public interface IMetricsServiceFactory
{
    IMetricsService CreateMetricsService(string moduleName);
}
