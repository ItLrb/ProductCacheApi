using Microsoft.EntityFrameworkCore;
using ProductCacheApi.Features.Products;

namespace ProductCacheApi.Config;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.Name).HasMaxLength(200);
            entity.Property(p => p.Price).HasPrecision(18, 2);
        });
    }
}
