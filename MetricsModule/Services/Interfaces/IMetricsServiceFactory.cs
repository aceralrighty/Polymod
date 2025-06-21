namespace TBD.MetricsModule.Services.Interfaces;

public interface IMetricsServiceFactory
{
    IMetricsService CreateMetricsService(string moduleName);
}
