using Serilog;
using ILogger = Serilog.ILogger;

namespace TBD.MetricsModule.Services;

public class MetricsService : IMetricsService
{
    private readonly ILogger _metricsLogger;

    public MetricsService()
    {
        _metricsLogger = new LoggerConfiguration()
            .WriteTo.File("Logs/metrics.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }

    public void IncrementCounter(string key)
    {
        MetricsCollector.Instance.Increment(key);
        var newValue = MetricsCollector.Instance.Get(key);
        _metricsLogger.Information("Metric incremented: {MetricKey} = {NewValue}", key, newValue);
    }

    public int GetCount(string key) => MetricsCollector.Instance.Get(key);

    public Dictionary<string, int> GetAllMetrics()
    {
        var metrics = MetricsCollector.Instance.GetAll();
        foreach (var (key, value) in metrics)
        {
            _metricsLogger.Information("Metric: {Key} = {Value}", key, value);
        }

        return metrics;
    }
}
