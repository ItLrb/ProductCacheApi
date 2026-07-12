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

    public async Task<CacheResult<IReadOnlyList<ProductDto>>> GetAll()
    {
        var cached = await _cache.GetAsync<List<ProductDto>>(ProductListCacheKey);
        if (cached is not null)
            return CacheResult.Hit<IReadOnlyList<ProductDto>>(cached);

        var products = await _context.Products
            .AsNoTracking()
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                Stock = p.Stock,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        await _cache.SetAsync(ProductListCacheKey, products, CacheTtl);

        _logger.LogInformation("Loaded {Count} products from the database", products.Count);
        return CacheResult.Miss<IReadOnlyList<ProductDto>>(products);
    }

    public async Task<CacheResult<ProductDto>?> GetById(int id)
    {
        var cacheKey = ProductCacheKey(id);

        var cached = await _cache.GetAsync<ProductDto>(cacheKey);
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
            .FirstOrDefaultAsync();

        if (product is null)
            return null;

        await _cache.SetAsync(cacheKey, product, CacheTtl);

        _logger.LogInformation("Loaded product {ProductId} from the database", product.Id);
        return CacheResult.Miss(product);
    }

    public async Task<ProductDto> Create(CreateProductDto dto)
    {
        var product = new Product
        {
            Name = dto.Name,
            Price = dto.Price,
            Stock = dto.Stock,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        await _cache.RemoveAsync(ProductListCacheKey);

        _logger.LogInformation("Product {ProductId} was created", product.Id);
        return ToDto(product);
    }

    public async Task<Result<ProductDto>> Update(int id, UpdateProductDto dto)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null)
            return Result<ProductDto>.Failure($"Product with ID {id} not found");

        product.Name = dto.Name;
        product.Price = dto.Price;
        product.Stock = dto.Stock;

        await _context.SaveChangesAsync();

        await _cache.RemoveAsync(ProductListCacheKey);
        await _cache.RemoveAsync(ProductCacheKey(id));

        _logger.LogInformation("Product {ProductId} was updated", product.Id);
        return Result<ProductDto>.Success(ToDto(product));
    }

    public async Task<Result<bool>> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null)
            return Result<bool>.Failure($"Product with ID {id} not found");

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        await _cache.RemoveAsync(ProductListCacheKey);
        await _cache.RemoveAsync(ProductCacheKey(id));

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
