using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ProductCacheApi.Config;
using ProductCacheApi.Features.Products;
using ProductCacheApi.Features.Products.DTOs;
using Xunit;

namespace ProductCacheApi.Tests;

public class ProductServiceTests
{
    private const string ListCacheKey = "products:all";

    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ProductService NewService(AppDbContext ctx, FakeCacheService cache) =>
        new(ctx, cache, NullLogger<ProductService>.Instance);

    private static async Task<Product> SeedProduct(AppDbContext ctx, string name = "Widget", decimal price = 10m)
    {
        var product = new Product { Name = name, Price = price, Stock = 5 };
        ctx.Products.Add(product);
        await ctx.SaveChangesAsync();
        return product;
    }

    [Fact]
    public async Task GetAll_OnCacheMiss_ReportsDatabaseSource()
    {
        await using var ctx = NewContext();
        var cache = new FakeCacheService();
        await SeedProduct(ctx);
        var service = NewService(ctx, cache);

        var result = await service.GetAll(page: 1, pageSize: 20);

        Assert.False(result.FromCache); // regression: used to always report "cache"
        Assert.Single(result.Value.Items);
        Assert.Equal(1, result.Value.TotalItems);
        Assert.True(cache.Contains(ListCacheKey)); // list gets populated for next time
    }

    [Fact]
    public async Task GetAll_OnCacheHit_ReportsCacheSource()
    {
        await using var ctx = NewContext();
        var cache = new FakeCacheService();
        cache.Seed(ListCacheKey, new List<ProductDto>
        {
            new() { Id = 1, Name = "Cached", Price = 1m, Stock = 1 }
        });
        var service = NewService(ctx, cache);

        var result = await service.GetAll(page: 1, pageSize: 20);

        Assert.True(result.FromCache);
        Assert.Equal("Cached", Assert.Single(result.Value.Items).Name);
    }

    [Fact]
    public async Task GetAll_PagesTheResults()
    {
        await using var ctx = NewContext();
        for (var i = 0; i < 25; i++)
            ctx.Products.Add(new Product { Name = $"P{i:00}", Price = 1m, Stock = 1 });
        await ctx.SaveChangesAsync();
        var service = NewService(ctx, new FakeCacheService());

        var page1 = await service.GetAll(page: 1, pageSize: 10);
        var page3 = await service.GetAll(page: 3, pageSize: 10);

        Assert.Equal(25, page1.Value.TotalItems);
        Assert.Equal(3, page1.Value.TotalPages);
        Assert.Equal(10, page1.Value.Items.Count);
        Assert.Equal(5, page3.Value.Items.Count); // last page has the remainder
        // Stable ordering by Id -> pages do not overlap
        Assert.Empty(page1.Value.Items.Select(p => p.Id).Intersect(page3.Value.Items.Select(p => p.Id)));
    }

    [Fact]
    public async Task Update_WhenMissing_ReturnsNotFoundErrorType()
    {
        await using var ctx = NewContext();
        var service = NewService(ctx, new FakeCacheService());

        var result = await service.Update(7, new UpdateProductDto { Name = "X", Price = 1m, Stock = 1 });

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultError.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Create_InvalidatesTheListCacheKey_NotAStringLiteral()
    {
        await using var ctx = NewContext();
        var cache = new FakeCacheService();
        var service = NewService(ctx, cache);

        await service.Create(new CreateProductDto { Name = "New", Price = 9.99m, Stock = 3 });

        // regression: it used to remove the literal "ProductListCacheKey"
        Assert.Contains(ListCacheKey, cache.RemovedKeys);
        Assert.DoesNotContain("ProductListCacheKey", cache.RemovedKeys);
    }

    [Fact]
    public async Task Create_PersistsProduct_AndReturnsGeneratedId()
    {
        await using var ctx = NewContext();
        var service = NewService(ctx, new FakeCacheService());

        var dto = await service.Create(new CreateProductDto { Name = "Gadget", Price = 5m, Stock = 2 });

        Assert.True(dto.Id > 0);
        Assert.Equal(1, await ctx.Products.CountAsync());
    }

    [Fact]
    public async Task GetById_WhenMissing_ReturnsNull()
    {
        await using var ctx = NewContext();
        var service = NewService(ctx, new FakeCacheService());

        var result = await service.GetById(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task Update_WhenMissing_ReturnsFailure()
    {
        await using var ctx = NewContext();
        var service = NewService(ctx, new FakeCacheService());

        var result = await service.Update(42, new UpdateProductDto { Name = "X", Price = 1m, Stock = 1 });

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task Update_WhenPresent_UpdatesAndInvalidatesBothKeys()
    {
        await using var ctx = NewContext();
        var cache = new FakeCacheService();
        var product = await SeedProduct(ctx);
        var service = NewService(ctx, cache);

        var result = await service.Update(product.Id,
            new UpdateProductDto { Name = "Renamed", Price = 20m, Stock = 8 });

        Assert.True(result.IsSuccess);
        Assert.Equal("Renamed", result.Data!.Name);
        Assert.Contains(ListCacheKey, cache.RemovedKeys);
        Assert.Contains($"product:{product.Id}", cache.RemovedKeys);
    }

    [Fact]
    public async Task Delete_WhenPresent_RemovesRowAndInvalidatesCache()
    {
        await using var ctx = NewContext();
        var cache = new FakeCacheService();
        var product = await SeedProduct(ctx);
        var service = NewService(ctx, cache);

        var result = await service.Delete(product.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, await ctx.Products.CountAsync());
        Assert.Contains(ListCacheKey, cache.RemovedKeys);
        Assert.Contains($"product:{product.Id}", cache.RemovedKeys);
    }

    [Fact]
    public async Task Delete_WhenMissing_ReturnsFailure()
    {
        await using var ctx = NewContext();
        var service = NewService(ctx, new FakeCacheService());

        var result = await service.Delete(123);

        Assert.False(result.IsSuccess);
    }
}
