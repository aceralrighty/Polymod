using System.Collections.Concurrent;

namespace TBD.MetricsModule.Services;

public sealed class MetricsCollector
{
    private static readonly Lazy<MetricsCollector> _instance =
        new(() => new MetricsCollector());

    private readonly ConcurrentDictionary<string, int> _counters = new();

    private MetricsCollector() { }

    public static MetricsCollector Instance => _instance.Value;

    public void Increment(string key)
    {
        _counters.AddOrUpdate(key, 1, (_, oldValue) => oldValue + 1);
    }

    public int Get(string key)
    {
        return _counters.TryGetValue(key, out var value) ? value : 0;
    }

    public Dictionary<string, int> GetAll() => new(_counters);
}
