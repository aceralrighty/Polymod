namespace TBD.MetricsModule.Services;

public interface IMetricsService
{
    void IncrementCounter(string key);
    int GetCount(string key);
    Dictionary<string, int> GetAllMetrics();
}
