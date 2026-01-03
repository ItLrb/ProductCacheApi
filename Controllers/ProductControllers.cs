using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductCacheApi.Cache;
using ProductCacheApi.Entities;
using ProductCacheApi.DbContext;
using ProductCacheApi.Interfaces;

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
        
        _logger.LogInformation("All products was triggered sucgcessfully");
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
    public async Task<IActionResult> Create(Product product)
    {
        _logger.LogInformation("Initializating the creation of product {@Product}", product);
        
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        await _cache.RemoveAsync(ProductListCacheKey);
        
        _logger.LogInformation("Product created successfully : \nId {ProductId} \nProduct {@Product}", product.Id, product);
        
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Product product)
    {
        if (id != product.Id)
            return BadRequest();
        
        _context.Entry(product).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        await _cache.RemoveAsync(ProductListCacheKey);
        await _cache.RemoveAsync($"product:{id}");

        _logger.LogInformation("Product with ID : {ProductId} was update successfully", product.Id);
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

        _logger.LogInformation("Product with ID {ProductId} by the name {Product} was successfully deleted", product.Id, product);
        return NoContent();
    }
}