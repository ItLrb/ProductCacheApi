namespace ProductCacheApi.DTOs;

public record ProductResponseDto(
    string Source, 
    ProductDto Data
);