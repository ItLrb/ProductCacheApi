using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    public ProductControllers(AppDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
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

        return Ok(new { source = "database", data = products });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var cacheKey = $"product:{id}";

        var cachedProduct = await _cache.GetAsync<Product>(cacheKey);
        if (cachedProduct is not null)
            return Ok(new { source = "cache", data = cachedProduct });

        var product = await _context.Products.FindAsync(id);
        if (product is null)
            return NotFound();

        await _cache.SetAsync(cacheKey, product, TimeSpan.FromMinutes(5));

        return Ok(new { source = "database", data = product });
    }

    [HttpPost]
    public async Task<IActionResult> Create(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        await _cache.RemoveAsync(ProductListCacheKey);

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

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null)
            return NotFound();
        
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        await _cache.RemoveAsync(ProductListCacheKey);
        await _cache.RemoveAsync($"product:{id}");

        return NoContent();
    }
}