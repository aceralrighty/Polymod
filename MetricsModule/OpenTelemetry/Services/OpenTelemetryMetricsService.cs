using System.Diagnostics.Metrics;
using TBD.MetricsModule.Services;
using TBD.MetricsModule.Services.Interfaces;

namespace TBD.MetricsModule.OpenTelemetry.Services;

public class OpenTelemetryMetricsService(string moduleName) : IMetricsService
{
    private readonly IMetricsService _baseService = new MetricsService(moduleName);
    private readonly Meter _meter = new($"TBD.{moduleName}", "1.0.0");
    private readonly Dictionary<string, Counter<int>> _counters = new();
    private readonly Dictionary<string, Histogram<double>> _histograms = new();

    public void IncrementCounter(string key)
    {
        // Use your existing implementation
        _baseService.IncrementCounter(key);

        // Create OpenTelemetry counter if it doesn't exist
        if (!_counters.ContainsKey(key))
        {
            _counters[key] = _meter.CreateCounter<int>(
                name: SanitizeMetricName(key),
                description: $"Counter for {key}"
            );
        }

        _counters[key].Add(1);
    }

    public void RecordHistogram(string key, double value, params KeyValuePair<string, object?>[] tags)
    {
        // Create OpenTelemetry histogram if it doesn't exist
        if (!_histograms.ContainsKey(key))
        {
            _histograms[key] = _meter.CreateHistogram<double>(
                name: SanitizeMetricName(key),
                description: $"Histogram for {key}"
            );
        }

        _histograms[key].Record(value, tags);
    }

    public int GetCount(string key) => _baseService.GetCount(key);

    public Dictionary<string, int> GetAllMetrics() => _baseService.GetAllMetrics();

    private static string SanitizeMetricName(string name)
    {
        return name.Replace(".", "_").ToLower();
    }
}
