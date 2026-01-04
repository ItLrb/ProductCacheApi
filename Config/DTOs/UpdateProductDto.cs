using System.ComponentModel.DataAnnotations;

namespace ProductCacheApi.DTOs;

public class UpdateProductDto
{
    [Required] public string Name { get; set; } = string.Empty;
    [Range(0.01, double.MaxValue)] public decimal Price { get; set; }
    [Range(0, int.MaxValue)] public int Stock { get; set; }
}

