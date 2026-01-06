using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductCacheApi.Cache;
using ProductCacheApi.Entities;
using ProductCacheApi.DbContext;
using ProductCacheApi.DTOs;
using ProductCacheApi.Features.Products;
using ProductCacheApi.Interfaces;
using ProductCacheApi.Middlewares;
using ProductCacheApi.Responses;

namespace ProductCacheApi.Controllers;

public class ProductService
{
    private readonly AppDbContext _context;
    private readonly ICacheService _cache;
    private readonly ILogger<ProductService> _logger;
    
    public ProductService(AppDbContext context, ICacheService cache, ILogger<ProductService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    private const string ProductListCacheKey = "products:all";
    public async Task<ProductsResponseDto> GetAll()
    {
        var cachedProducts = await _cache.GetAsync<List<ProductDto>>(ProductListCacheKey);

        if (cachedProducts is not null) 
            return new ProductsResponseDto("cache", cachedProducts);
        
        var products = await _context.Products.AsNoTracking().ToListAsync();
        
        var productsDto = products.Select(p => new ProductDto
        {
           Id = p.Id, Name = p.Name, Price = p.Price, Stock = p.Stock
        }).ToList();
        
        await _cache.SetAsync(ProductListCacheKey, productsDto, TimeSpan.FromMinutes(5));
        
        _logger.LogInformation("All products was triggered successfully");
        return new ProductsResponseDto("cache", productsDto);
    }

    public async Task<ProductResponseDto?> GetById(int id)
    {
        _logger.LogInformation("Product request by ID were triggered");
        
        var cacheKey = $"product:{id}";

        var cachedProduct = await _cache.GetAsync<ProductDto>(cacheKey);
        if (cachedProduct is not null)
            return new ProductResponseDto("cache", cachedProduct);

        var product = await _context.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                Stock = p.Stock
            })
            .FirstOrDefaultAsync();

        if (product is null)
            return null;

        await _cache.SetAsync(
            cacheKey,
            product,
            TimeSpan.FromMinutes(5)
        );

        _logger.LogInformation("Product requested by ID {ProductID}", product.Id);
        return new ProductResponseDto("database", product);
    }

    public async Task<Result<ProductDto>> Create(CreateProductDto dto)
    {
        if (dto.Price <= 0) 
            return Result<ProductDto>.Failure("The price can't be less than 0");

        var product = new Entity 
        { 
            Name = dto.Name, 
            Price = dto.Price, 
            Stock = dto.Stock, 
            CreatedAt = DateTime.UtcNow 
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync("ProductListCacheKey");

        var response = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            Stock = product.Stock
        };
        
        return Result<ProductDto>.Success(response);
    }

    public async Task<Result<ProductDto>> Update(int id, UpdateProductDto dto)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return Result<ProductDto>.Failure("Product not found");
        
        product.Name = dto.Name;
        product.Price = dto.Price;
        product.Stock = dto.Stock;
        
        await _context.SaveChangesAsync();

        await _cache.RemoveAsync(ProductListCacheKey);
        await _cache.RemoveAsync($"product:{id}");

        _logger.LogInformation("Product with ID {ProductId} was updated successfully", product.Id);

        var response = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            Stock = product.Stock
        };
        
        return Result<ProductDto>.Success(response);
    }

    public async Task<Result<bool>> Delete(int id)
    {
        _logger.LogInformation("Delete product by ID was requested");
        
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return Result<bool>.Failure($"Product with ID {id} not found");
        
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        await _cache.RemoveAsync(ProductListCacheKey);
        await _cache.RemoveAsync($"product:{id}");

        _logger.LogInformation("Product with ID {ProductId} by the name {ProductName} was successfully deleted", product.Id, product.Name);
        return Result<bool>.Success(true);
    }
}