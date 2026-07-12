namespace ProductCacheApi.Features.Products;

/// <summary>
/// Wraps a value together with whether it was served from the cache,
/// so the controller can expose it as an <c>X-Cache: HIT|MISS</c> header
/// instead of leaking a "source" field into the response body.
/// </summary>
public record CacheResult<T>(T Value, bool FromCache);

public static class CacheResult
{
    public static CacheResult<T> Hit<T>(T value) => new(value, true);
    public static CacheResult<T> Miss<T>(T value) => new(value, false);
}
