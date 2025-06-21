namespace TBD.MetricsModule.Services.Interfaces;

public interface IMetricsService
{
    void IncrementCounter(string key);
    int GetCount(string key);
    Dictionary<string, int> GetAllMetrics();
}
