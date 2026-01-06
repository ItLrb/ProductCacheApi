namespace ProductCacheApi.DTOs;

public record ProductsResponseDto(
    string Source, 
    List<ProductDto?> Data
    );