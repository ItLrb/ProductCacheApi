using Microsoft.AspNetCore.Mvc;
using ProductCacheApi.Features.Products.DTOs;

namespace ProductCacheApi.Features.Products;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly ProductService _productService;

    public ProductController(ProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var result = await _productService.GetAll(page, pageSize, cancellationToken);
        SetCacheHeader(result.FromCache);
        return Ok(result.Value);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _productService.GetById(id, cancellationToken);
        if (result is null)
            return Problem(detail: $"Product with ID {id} not found",
                statusCode: StatusCodes.Status404NotFound, title: "Resource not found");

        SetCacheHeader(result.FromCache);
        return Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductDto>> Create(CreateProductDto dto, CancellationToken cancellationToken)
    {
        var product = await _productService.Create(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> Update(int id, UpdateProductDto dto, CancellationToken cancellationToken)
    {
        var result = await _productService.Update(id, dto, cancellationToken);
        if (!result.IsSuccess)
            return ToProblem(result);

        return Ok(result.Data);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _productService.Delete(id, cancellationToken);
        if (!result.IsSuccess)
            return ToProblem(result);

        return NoContent();
    }

    private void SetCacheHeader(bool fromCache)
        => Response.Headers["X-Cache"] = fromCache ? "HIT" : "MISS";

    private ObjectResult ToProblem<T>(Result<T> result)
    {
        var (status, title) = result.ErrorType switch
        {
            ResultError.NotFound => (StatusCodes.Status404NotFound, "Resource not found"),
            ResultError.Validation => (StatusCodes.Status400BadRequest, "Validation failed"),
            ResultError.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (StatusCodes.Status400BadRequest, "Request failed")
        };

        return Problem(detail: result.Error, statusCode: status, title: title);
    }
}
