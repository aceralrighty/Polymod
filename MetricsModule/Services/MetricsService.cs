using Serilog;
using Serilog.Formatting.Display;
using ILogger = Serilog.ILogger;

namespace TBD.MetricsModule.Services;

public class MetricsService(string moduleName) : IMetricsService
{
    private readonly ILogger _metricsLogger = new LoggerConfiguration()
        .WriteTo.File(
            path: $"Logs/{moduleName.ToLower()}-metrics.log",
            rollingInterval: RollingInterval.Minute,
            shared: false, // Prevent file sharing between loggers
            fileSizeLimitBytes: 50 * 1024 * 1024, // 50MB limit per file
            rollOnFileSizeLimit: true, // Roll when the size limit is reached
            retainedFileCountLimit: 10
        )
        .CreateLogger();



    public void IncrementCounter(string key)
    {
        MetricsCollector.Instance.Increment(key);
        var newValue = MetricsCollector.Instance.Get(key);
        _metricsLogger.Information("Metric incremented: {MetricKey}:-> {NewValue}", key, newValue);
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
