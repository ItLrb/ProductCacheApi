using Microsoft.AspNetCore.Mvc;
using ProductCacheApi.Features.Products.DTOs;

namespace ProductCacheApi.Features.Products;

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
    [ProducesResponseType(typeof(IReadOnlyList<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetAll()
    {
        var result = await _productService.GetAll();
        SetCacheHeader(result.FromCache);
        return Ok(result.Value);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        var result = await _productService.GetById(id);
        if (result is null)
            return NotFoundProblem($"Product with ID {id} not found");

        SetCacheHeader(result.FromCache);
        return Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductDto>> Create(CreateProductDto dto)
    {
        var product = await _productService.Create(dto);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> Update(int id, UpdateProductDto dto)
    {
        var result = await _productService.Update(id, dto);
        if (!result.IsSuccess)
            return NotFoundProblem(result.Error!);

        return Ok(result.Data);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _productService.Delete(id);
        if (!result.IsSuccess)
            return NotFoundProblem(result.Error!);

        return NoContent();
    }

    private void SetCacheHeader(bool fromCache)
        => Response.Headers["X-Cache"] = fromCache ? "HIT" : "MISS";

    private ObjectResult NotFoundProblem(string detail)
        => Problem(detail: detail, statusCode: StatusCodes.Status404NotFound, title: "Resource not found");
}
