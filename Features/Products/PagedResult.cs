namespace ProductCacheApi.Features.Products;

/// <summary>A single page of results plus the metadata a client needs to page through the rest.</summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalItems)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);
}
