using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using TBD.MetricsModule.Services;
using TBD.MetricsModule.Services.Interfaces;

namespace TBD.MetricsModule.OpenTelemetry.Services;

public class OpenTelemetryMetricsService(string moduleName) : IMetricsService
{
    private readonly IMetricsService _baseService = new MetricsService(moduleName);
    private readonly Meter _meter = new($"TBD.{moduleName}", "1.0.0");
    private readonly ConcurrentDictionary<string, Counter<int>> _counters = new();
    private readonly ConcurrentDictionary<string, Histogram<double>> _histograms = new();

    public void IncrementCounter(string key)
    {
        // Use your existing implementation for text logging
        _baseService.IncrementCounter(key);

        // Create OpenTelemetry counter if it doesn't exist, or get existing one
        var counter = _counters.GetOrAdd(key, k =>
            _meter.CreateCounter<int>(
                name: SanitizeMetricName(k),
                description: $"Counter for {k}")
        );

        counter.Add(1);
    }

    public void RecordHistogram(string key, double value, params KeyValuePair<string, object?>[] tags)
    {
        // Create OpenTelemetry histogram if it doesn't exist, or get existing one
        var histogram = _histograms.GetOrAdd(key, k =>
            _meter.CreateHistogram<double>(
                name: SanitizeMetricName(k),
                description: $"Histogram for {k}")
        );

        histogram.Record(value, tags);
    }

    public int GetCount(string key) => _baseService.GetCount(key);

    public Dictionary<string, int> GetAllMetrics() => _baseService.GetAllMetrics();

    private static string SanitizeMetricName(string name)
    {
        return name.Replace(".", "_").ToLower();
    }
}
