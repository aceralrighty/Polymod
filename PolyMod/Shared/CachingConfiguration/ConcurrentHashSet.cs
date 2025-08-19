using System.Collections.Concurrent;

namespace TBD.Shared.CachingConfiguration;

public class ConcurrentHashSet<T> : IDisposable where T : notnull
{
    private readonly ConcurrentDictionary<T, byte> _dictionary = new();

    public bool Add(T item) => _dictionary.TryAdd(item, 0);
    public bool TryRemove(T item) => _dictionary.TryRemove(item, out _);
    public bool Contains(T item) => _dictionary.ContainsKey(item);
    public void Clear() => _dictionary.Clear();
    public IList<T> ToList() => _dictionary.Keys.ToList();

    public void Dispose()
    {
        _dictionary.Clear();
        GC.SuppressFinalize(this);
    }
}
