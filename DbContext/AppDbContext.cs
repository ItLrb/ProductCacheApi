using Microsoft.EntityFrameworkCore;
using ProductCacheApi.Entities;

namespace ProductCacheApi.DbContext;

public class AppDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }   
    
    public DbSet<Product> Products { get; set; }
}