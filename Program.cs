using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using ProductCacheApi.DbContext;
using ProductCacheApi.Interfaces;
using ProductCacheApi.Cache;
using DotNetEnv;

Env.Load();
var dbConnection = Environment.GetEnvironmentVariable("DefaultConnection");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString(dbConnection),
        ServerVersion.AutoDetect(
            builder.Configuration.GetConnectionString(dbConnection)
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