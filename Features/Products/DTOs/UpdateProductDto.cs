using System.ComponentModel.DataAnnotations;

namespace ProductCacheApi.Features.Products.DTOs;

public record UpdateProductDto
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal Price { get; init; }

    [Range(0, int.MaxValue)]
    public int Stock { get; init; }
}
