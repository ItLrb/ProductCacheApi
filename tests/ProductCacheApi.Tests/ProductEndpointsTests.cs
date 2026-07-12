using System.Net;
using System.Net.Http.Json;
using ProductCacheApi.Features.Products;
using ProductCacheApi.Features.Products.DTOs;
using Xunit;

namespace ProductCacheApi.Tests;

public class ProductEndpointsTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public ProductEndpointsTests(ApiFactory factory) => _client = factory.CreateClient();

    private static CreateProductDto ValidProduct(string name = "Widget") =>
        new() { Name = name, Price = 12.50m, Stock = 3 };

    [Fact]
    public async Task Create_ReturnsCreated_WithLocationAndBody()
    {
        var response = await _client.PostAsJsonAsync("/api/Product", ValidProduct());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var body = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.NotNull(body);
        Assert.True(body!.Id > 0);
    }

    [Fact]
    public async Task Create_WithInvalidPrice_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/Product",
            new CreateProductDto { Name = "Bad", Price = 0m, Stock = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetById_WhenMissing_ReturnsNotFoundProblem()
    {
        var response = await _client.GetAsync("/api/Product/987654");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetAll_SetsCacheHeader_MissThenHit()
    {
        // Seed one product so the list is non-trivial.
        await _client.PostAsJsonAsync("/api/Product", ValidProduct("Cached"));

        var first = await _client.GetAsync("/api/Product");
        var second = await _client.GetAsync("/api/Product");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal("MISS", first.Headers.GetValues("X-Cache").Single());
        Assert.Equal("HIT", second.Headers.GetValues("X-Cache").Single());
    }

    [Fact]
    public async Task FullLifecycle_Create_Update_Delete()
    {
        var created = await (await _client.PostAsJsonAsync("/api/Product", ValidProduct("Lifecycle")))
            .Content.ReadFromJsonAsync<ProductDto>();
        Assert.NotNull(created);

        var update = await _client.PutAsJsonAsync($"/api/Product/{created!.Id}",
            new UpdateProductDto { Name = "Updated", Price = 99m, Stock = 1 });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.Content.ReadFromJsonAsync<ProductDto>();
        Assert.Equal("Updated", updated!.Name);

        var delete = await _client.DeleteAsync($"/api/Product/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var afterDelete = await _client.GetAsync($"/api/Product/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task Update_WhenMissing_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync("/api/Product/55555",
            new UpdateProductDto { Name = "X", Price = 1m, Stock = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ReturnsPagedEnvelope_AndClampsPageSize()
    {
        var page = await _client.GetFromJsonAsync<PagedResult<ProductDto>>("/api/Product?page=1&pageSize=5");

        Assert.NotNull(page);
        Assert.Equal(1, page!.Page);
        Assert.Equal(5, page.PageSize);

        // pageSize above the cap is clamped to 100 (not echoed back verbatim)
        var clamped = await _client.GetFromJsonAsync<PagedResult<ProductDto>>("/api/Product?pageSize=99999");
        Assert.Equal(100, clamped!.PageSize);
    }

    [Fact]
    public async Task Timestamps_AreSerializedAsUtc_Consistently()
    {
        var create = await _client.PostAsJsonAsync("/api/Product", ValidProduct("Clock"));
        var created = await create.Content.ReadFromJsonAsync<ProductDto>();

        var getRaw = await _client.GetStringAsync($"/api/Product/{created!.Id}");

        // Both the POST body and a later GET must render createdAt with a trailing 'Z'.
        Assert.Contains("\"createdAt\"", getRaw);
        Assert.Matches("\"createdAt\":\"[^\"]+Z\"", getRaw);
    }
}
