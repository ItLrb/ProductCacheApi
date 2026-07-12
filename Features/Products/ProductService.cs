using Microsoft.EntityFrameworkCore;
using ProductCacheApi.Config;
using ProductCacheApi.Features.Cache;
using ProductCacheApi.Features.Products.DTOs;

namespace ProductCacheApi.Features.Products;

public class ProductService
{
    private const string ProductListCacheKey = "products:all";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly AppDbContext _context;
    private readonly ICacheService _cache;
    private readonly ILogger<ProductService> _logger;

    public ProductService(AppDbContext context, ICacheService cache, ILogger<ProductService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    private static string ProductCacheKey(int id) => $"product:{id}";

    public async Task<CacheResult<PagedResult<ProductDto>>> GetAll(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (products, fromCache) = await GetAllProducts(cancellationToken);

        var items = products
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var paged = new PagedResult<ProductDto>(items, page, pageSize, products.Count);
        return new CacheResult<PagedResult<ProductDto>>(paged, fromCache);
    }

    // The full catalog is cached under a single key and paged in memory. This keeps cache
    // invalidation trivial (one key to drop on writes) and is well suited to a product
    // catalog; a very large, high-churn dataset would call for DB-side paging with a
    // generation-based cache key instead.
    private async Task<(List<ProductDto> Products, bool FromCache)> GetAllProducts(CancellationToken cancellationToken)
    {
        var cached = await _cache.GetAsync<List<ProductDto>>(ProductListCacheKey, cancellationToken);
        if (cached is not null)
            return (cached, true);

        var products = await _context.Products
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                Stock = p.Stock,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(cancellationToken);

        await _cache.SetAsync(ProductListCacheKey, products, CacheTtl, cancellationToken);

        _logger.LogInformation("Loaded {Count} products from the database", products.Count);
        return (products, false);
    }

    public async Task<CacheResult<ProductDto>?> GetById(int id, CancellationToken cancellationToken = default)
    {
        var cacheKey = ProductCacheKey(id);

        var cached = await _cache.GetAsync<ProductDto>(cacheKey, cancellationToken);
        if (cached is not null)
            return CacheResult.Hit(cached);

        var product = await _context.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                Stock = p.Stock,
                CreatedAt = p.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (product is null)
            return null;

        await _cache.SetAsync(cacheKey, product, CacheTtl, cancellationToken);

        _logger.LogInformation("Loaded product {ProductId} from the database", product.Id);
        return CacheResult.Miss(product);
    }

    public async Task<ProductDto> Create(CreateProductDto dto, CancellationToken cancellationToken = default)
    {
        var product = new Product
        {
            Name = dto.Name,
            Price = dto.Price,
            Stock = dto.Stock,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync(ProductListCacheKey, cancellationToken);

        _logger.LogInformation("Product {ProductId} was created", product.Id);
        return ToDto(product);
    }

    public async Task<Result<ProductDto>> Update(int id, UpdateProductDto dto, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FindAsync([id], cancellationToken);
        if (product is null)
            return Result<ProductDto>.Failure($"Product with ID {id} not found", ResultError.NotFound);

        product.Name = dto.Name;
        product.Price = dto.Price;
        product.Stock = dto.Stock;

        await _context.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync(ProductListCacheKey, cancellationToken);
        await _cache.RemoveAsync(ProductCacheKey(id), cancellationToken);

        _logger.LogInformation("Product {ProductId} was updated", product.Id);
        return Result<ProductDto>.Success(ToDto(product));
    }

    public async Task<Result<bool>> Delete(int id, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FindAsync([id], cancellationToken);
        if (product is null)
            return Result<bool>.Failure($"Product with ID {id} not found", ResultError.NotFound);

        _context.Products.Remove(product);
        await _context.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync(ProductListCacheKey, cancellationToken);
        await _cache.RemoveAsync(ProductCacheKey(id), cancellationToken);

        _logger.LogInformation("Product {ProductId} ({ProductName}) was deleted", product.Id, product.Name);
        return Result<bool>.Success(true);
    }

    private static ProductDto ToDto(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Price = p.Price,
        Stock = p.Stock,
        CreatedAt = p.CreatedAt
    };
}
