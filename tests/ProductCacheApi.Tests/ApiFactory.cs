using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProductCacheApi.Config;

namespace ProductCacheApi.Tests;

/// <summary>
/// Boots the real HTTP pipeline (controllers, filters, ProblemDetails, exception handler)
/// but swaps the MySQL-backed <see cref="AppDbContext"/> for an isolated in-memory store,
/// so integration tests exercise the actual API contract without a database.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // A non-empty connection string is required for the app's startup guard to pass;
        // it is never actually used because the DbContext is replaced below.
        builder.UseSetting("ConnectionStrings:DefaultConnection", "server=unused;database=unused;user=unused;password=unused");
        builder.UseSetting("Redis:Connection", "");
        builder.UseEnvironment("Production");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_dbName));
        });
    }
}
