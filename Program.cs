using Microsoft.EntityFrameworkCore;
using ProductCacheApi.Config;
using ProductCacheApi.Features.Cache;
using ProductCacheApi.Features.Products;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(path: "Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// The database is mandatory: fail fast at startup with a clear message instead of
// surfacing confusing per-request 500s when it is missing.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is not configured. " +
        "Set ConnectionStrings__DefaultConnection (environment variable) or use user-secrets.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        // Pin the server version instead of AutoDetect: AutoDetect opens a connection at
        // startup and stalls (or crashes) the whole app when the database is unreachable.
        new MySqlServerVersion(new Version(8, 0, 36))));

// Cache degrades gracefully: use Redis when configured, otherwise an in-process cache so
// the API still runs. AbortOnConnectFail=false + short timeouts keep requests fast when
// a configured Redis is temporarily down, instead of hanging ~5s per call.
var redisConnection = builder.Configuration["Redis:Connection"];
if (string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddDistributedMemoryCache();
}
else
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        var config = ConfigurationOptions.Parse(redisConnection);
        config.AbortOnConnectFail = false;
        config.ConnectTimeout = 1000;
        config.SyncTimeout = 1000;
        config.ConnectRetry = 1;
        options.ConfigurationOptions = config;
    });
}

builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddScoped<ProductService>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Optionally apply pending migrations at startup. Disabled by default (production-safe);
// docker-compose turns it on so `docker compose up` yields a ready-to-use database.
if (app.Configuration.GetValue<bool>("ApplyMigrationsAtStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Exposed so the integration test project can reference the entry point via WebApplicationFactory.
public partial class Program;
