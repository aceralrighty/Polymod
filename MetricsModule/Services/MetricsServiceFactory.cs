namespace TBD.MetricsModule.Services;

public class MetricsServiceFactory : IMetricsServiceFactory
{
    public IMetricsService CreateMetricsService(string moduleName)
    {
        return new MetricsService(moduleName);
    }
}
