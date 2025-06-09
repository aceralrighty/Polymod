namespace TBD.MetricsModule.Services;

public class MetricsServiceFactory : IMetricsServiceFactory
{
    public IMetricsService CreateMetricsService(string fileName)
    {
        return new MetricsService(fileName);
    }
}
