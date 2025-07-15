using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using TBD.MetricsModule.Services;
using TBD.MetricsModule.Services.Interfaces;

namespace TBD.MetricsModule.OpenTelemetry.Services;

public class OpenTelemetryMetricsService : IMetricsService
{
    private readonly string _moduleName;
    private readonly IMetricsService _baseService;
    private readonly Meter _meter;
    private readonly ConcurrentDictionary<string, Counter<int>> _counters = new();
    private readonly ConcurrentDictionary<string, Histogram<double>> _histograms = new();

    public OpenTelemetryMetricsService(string moduleName)
    {
        _moduleName = moduleName;
        _baseService = new MetricsService(moduleName);
        _meter = new Meter($"TBD.{moduleName}", "1.0.0");

        Console.WriteLine($"[METRICS] ✅ Created OpenTelemetryMetricsService for module: {moduleName}");
        Console.WriteLine($"[METRICS] ✅ Meter name: TBD.{moduleName}");
    }

    public void IncrementCounter(string key)
    {
        Console.WriteLine($"[METRICS] 🔢 {_moduleName}: Incrementing counter '{key}'");

        // Use your existing implementation for text logging
        _baseService.IncrementCounter(key);

        // Create OpenTelemetry counter if it doesn't exist, or get existing one
        var counter = _counters.GetOrAdd(key, k =>
        {
            var sanitizedName = SanitizeMetricName(k);
            Console.WriteLine($"[METRICS] 🆕 {_moduleName}: Creating new counter '{sanitizedName}' for key '{k}'");
            var newCounter = _meter.CreateCounter<int>(
                name: sanitizedName,
                description: $"Counter for {k}");
            Console.WriteLine($"[METRICS] ✅ {_moduleName}: Counter '{sanitizedName}' created successfully");
            return newCounter;
        });

        counter.Add(1);
        Console.WriteLine($"[METRICS] ✅ {_moduleName}: Counter '{key}' incremented in OpenTelemetry");
    }

    public void RecordHistogram(string key, double value, params KeyValuePair<string, object?>[] tags)
    {
        Console.WriteLine($"[METRICS] 📊 {_moduleName}: Recording histogram '{key}' with value {value}");

        // Create OpenTelemetry histogram if it doesn't exist, or get existing one
        var histogram = _histograms.GetOrAdd(key, k =>
        {
            var sanitizedName = SanitizeMetricName(k);
            Console.WriteLine($"[METRICS] 🆕 {_moduleName}: Creating new histogram '{sanitizedName}' for key '{k}'");
            var newHistogram = _meter.CreateHistogram<double>(
                name: sanitizedName,
                description: $"Histogram for {k}");
            Console.WriteLine($"[METRICS] ✅ {_moduleName}: Histogram '{sanitizedName}' created successfully");
            return newHistogram;
        });

        histogram.Record(value, tags);
        Console.WriteLine($"[METRICS] ✅ {_moduleName}: Histogram '{key}' recorded with value {value}");
    }

    public int GetCount(string key) => _baseService.GetCount(key);

    public Dictionary<string, int> GetAllMetrics() => _baseService.GetAllMetrics();

    private static string SanitizeMetricName(string name)
    {
        var sanitized = name.Replace(".", "_").ToLower();
        return sanitized;
    }
}
