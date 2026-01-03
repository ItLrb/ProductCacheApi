using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using ProductCacheApi.DbContext;
using ProductCacheApi.Interfaces;
using ProductCacheApi.Cache;
// using DotNetEnv;
using Microsoft.Extensions.Configuration;

// Env.Load();
// var dbConnection = Environment.GetEnvironmentVariable("DefaultConnection");

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

string connectionString = config.GetSection("ConnectionStrings")["DefaultConnection"];

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString(connectionString),
        ServerVersion.AutoDetect(
            builder.Configuration.GetConnectionString(connectionString)
        )
    ));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:Connection"];
});
builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.Run();