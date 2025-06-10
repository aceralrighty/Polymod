using Serilog;
using Serilog.Formatting.Display;
using ILogger = Serilog.ILogger;

namespace TBD.MetricsModule.Services;

public class MetricsService(string moduleName) : IMetricsService
{
    private static string? _lastLogDate;
    private readonly ILogger _metricsLogger = new LoggerConfiguration()
        .WriteTo.File(
            path: $"Logs/{moduleName.ToLower()}-metrics.log",
            rollingInterval: RollingInterval.Day,
            shared: false, // Prevent file sharing between loggers
            fileSizeLimitBytes: 50 * 1024 * 1024, // 50MB limit per file
            rollOnFileSizeLimit: true, // Roll when the size limit is reached

            retainedFileCountLimit: 10, // Keep 10 files max
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}"
        )
        .CreateLogger();

    private void LogWithDaySpacing(string template, params object[] args)
    {
        var currentDate = DateTime.Now.ToString("yyyy-MM-dd");

        if (_lastLogDate != currentDate)
        {
            if (_lastLogDate != null) // Don't add spacing for the very first log
            {
                _metricsLogger.Information(""); // Empty line for day separation
                _metricsLogger.Information("=== {Date} ===", currentDate);
                _metricsLogger.Information(""); // Another empty line
            }
            _lastLogDate = currentDate;
        }

        _metricsLogger.Information(template, args);
    }

    public void IncrementCounter(string key)
    {
        MetricsCollector.Instance.Increment(key);
        var newValue = MetricsCollector.Instance.Get(key);
        LogWithDaySpacing("Metric incremented: {MetricKey} = {NewValue}", key, newValue);
    }

    public int GetCount(string key) => MetricsCollector.Instance.Get(key);

    public Dictionary<string, int> GetAllMetrics()
    {
        var metrics = MetricsCollector.Instance.GetAll();
        foreach (var (key, value) in metrics)
        {
            LogWithDaySpacing("Metric: {Key} = {Value}", key, value);
        }

        return metrics;
    }
}
