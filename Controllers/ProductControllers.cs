using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductCacheApi.Cache;
using ProductCacheApi.Entities;
using ProductCacheApi.DbContext;
using ProductCacheApi.DTOs;
using ProductCacheApi.Interfaces;
using ProductCacheApi.Responses;

namespace ProductCacheApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductControllers : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICacheService _cache;
    private readonly ILogger<ProductControllers> _logger;
    
    public ProductControllers(AppDbContext context, ICacheService cache, ILogger<ProductControllers> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    private const string ProductListCacheKey = "products:all";

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var cachedProducts = await _cache.GetAsync<List<Product>>(ProductListCacheKey);

        if (cachedProducts is not null) 
            return Ok(new { source = "cache", data = cachedProducts });
        
        var products = await _context.Products.AsNoTracking().ToListAsync();
        await _cache.SetAsync(ProductListCacheKey, products, TimeSpan.FromMinutes(5));
        
        _logger.LogInformation("All products was triggered successfully");
        return Ok(new { source = "database", data = products });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        _logger.LogInformation("Product request by ID were triggered");
        
        var cacheKey = $"product:{id}";

        var cachedProduct = await _cache.GetAsync<Product>(cacheKey);
        if (cachedProduct is not null)
            return Ok(new { source = "cache", data = cachedProduct });

        var product = await _context.Products.FindAsync(id);
        if (product is null)
            return NotFound();

        await _cache.SetAsync(cacheKey, product, TimeSpan.FromMinutes(5));

        _logger.LogInformation("Product requested by ID {ProductID}", product.Id);
        return Ok(new { source = "database", data = product });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateProductDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        _logger.LogInformation("Creating product {@Dto}", dto);
        
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

        var response = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            Stock = product.Stock
        };

        return CreatedAtAction(
            nameof(GetById),
            new { id = product.Id },
            ApiResponse<ProductDto>.Ok(response, "Created product successfully")
        );
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateProductDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var product = await _context.Products.FindAsync(id);
        if (product is null)
            return NotFound();
        
        product.Name = dto.Name;
        product.Price = dto.Price;
        product.Stock = dto.Stock;
        
        await _context.SaveChangesAsync();

        await _cache.RemoveAsync(ProductListCacheKey);
        await _cache.RemoveAsync($"product:{id}");

        _logger.LogInformation("Product with ID {ProductId} was updated successfully", product.Id);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        _logger.LogInformation("Delete product by ID was requested");
        
        var product = await _context.Products.FindAsync(id);
        if (product is null)
            return NotFound();
        
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        await _cache.RemoveAsync(ProductListCacheKey);
        await _cache.RemoveAsync($"product:{id}");

        _logger.LogInformation("Product with ID {ProductId} by the name {ProductName} was successfully deleted", product.Id, product.Name);
        return NoContent();
    }
}