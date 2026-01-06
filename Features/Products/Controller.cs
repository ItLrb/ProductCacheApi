using Microsoft.AspNetCore.Mvc;
using ProductCacheApi.DTOs;
using ProductCacheApi.Responses;

namespace ProductCacheApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly ProductService _productService;
    
    public ProductController(ProductService productService)
    {
        _productService = productService;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _productService.GetAll();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _productService.GetById(id);
        if (result is null)
            return NotFound();
        
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateProductDto dto)
    {
        var result = await _productService.Create(dto);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, result.Data);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateProductDto dto)
    {
        var result = await _productService.Update(id, dto);

        if (!result.IsSuccess)
            return NotFound(ApiResponse<string>.Fail(result.Error));
        
        return Ok(ApiResponse<ProductDto>.Ok(result.Data, "Product updated successfully"));
    }
    

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _productService.Delete(id);

        if (!result.IsSuccess)
            return NotFound(ApiResponse<string>.Fail(result.Error));
        
        return Ok(ApiResponse<bool>.Ok(true, "Product deleted successfully"));
    }
}