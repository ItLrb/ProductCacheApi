using System.Text.Json;
using ProductCacheApi.Features.Cache;

namespace ProductCacheApi.Tests;

/// <summary>
/// In-memory <see cref="ICacheService"/> that records every key that was removed,
/// so tests can assert cache invalidation happens against the right keys.
/// Values are round-tripped through JSON to mimic the real serialization boundary.
/// </summary>
public class FakeCacheService : ICacheService
{
    private readonly Dictionary<string, string> _store = new();
    public List<string> RemovedKeys { get; } = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(key, out var json))
            return Task.FromResult(JsonSerializer.Deserialize<T>(json));

        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        _store[key] = JsonSerializer.Serialize(value);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        RemovedKeys.Add(key);
        _store.Remove(key);
        return Task.CompletedTask;
    }

    public bool Contains(string key) => _store.ContainsKey(key);

    public void Seed<T>(string key, T value) => _store[key] = JsonSerializer.Serialize(value);
}
