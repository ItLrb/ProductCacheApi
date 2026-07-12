using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ProductCacheApi.Features.Cache;

/// <summary>
/// Reports the cache as <see cref="HealthStatus.Degraded"/> (not Unhealthy) when a round-trip
/// fails, because the cache is an optional optimization — the API stays fully functional
/// without it, so a cache outage must not flip the overall health to a 503.
/// </summary>
public class CacheHealthCheck : IHealthCheck
{
    private const string ProbeKey = "health:cache:probe";
    private readonly ICacheService _cache;

    public CacheHealthCheck(ICacheService cache) => _cache = cache;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var value = Guid.NewGuid().ToString("N");
        await _cache.SetAsync(ProbeKey, value, TimeSpan.FromSeconds(10), cancellationToken);
        var roundTripped = await _cache.GetAsync<string>(ProbeKey, cancellationToken);

        return roundTripped == value
            ? HealthCheckResult.Healthy("Cache round-trip succeeded")
            : HealthCheckResult.Degraded("Cache is unavailable; serving directly from the database");
    }
}
