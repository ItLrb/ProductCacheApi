using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace ProductCacheApi.Features.Cache;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var data = await _cache.GetStringAsync(key);
            if (data is null)
                return default;

            return JsonSerializer.Deserialize<T>(data);
        }
        catch (Exception ex)
        {
            // Cache is a best-effort optimization: never let a Redis outage break the request.
            _logger.LogWarning(ex, "Failed to get cache value for key: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };

            var json = JsonSerializer.Serialize(value);
            await _cache.SetStringAsync(key, json, options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set cache value for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _cache.RemoveAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove cache value for key: {Key}", key);
        }
    }
}
