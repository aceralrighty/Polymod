using System.Diagnostics.Metrics;
using TBD.MetricsModule.Services;
using TBD.MetricsModule.Services.Interfaces;

namespace TBD.MetricsModule.OpenTelemetry.Services;

public class OpenTelemetryMetricsService(string moduleName) : IMetricsService
{
    private readonly IMetricsService _baseService = new MetricsService(moduleName);
    private readonly Meter _meter = new($"TBD.{moduleName}", "1.0.0");
    private readonly Dictionary<string, ObservableGauge<int>> _gauges = new();

    public void IncrementCounter(string key)
    {
        // Use your existing implementation
        _baseService.IncrementCounter(key);

        // Create OpenTelemetry observable gauge if it doesn't exist
        if (!_gauges.ContainsKey(key))
        {
            _gauges[key] = _meter.CreateObservableGauge<int>(
                name: key.Replace(".", "_").ToLower(),
                description: $"Counter for {key}",
                observeValue: () => MetricsCollector.Instance.Get(key)
            );
        }
    }

    public int GetCount(string key) => _baseService.GetCount(key);

    public Dictionary<string, int> GetAllMetrics() => _baseService.GetAllMetrics();
}
