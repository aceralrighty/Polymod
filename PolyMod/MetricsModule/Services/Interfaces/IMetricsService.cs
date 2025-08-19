namespace TBD.MetricsModule.Services.Interfaces;

public interface IMetricsService
{
    void IncrementCounter(string key);
    void RecordHistogram(string key, double value, params KeyValuePair<string, object?>[] tags);
    int GetCount(string key);
    Dictionary<string, int> GetAllMetrics();
}
