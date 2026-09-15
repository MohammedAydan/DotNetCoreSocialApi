using DotNetEnv;
using Microsoft.OpenApi.Models;
using Social.Admin.Web;
using Social.API.Configuration;
using Social.API.Extensions;
using Social.API.Middlewares;
using Social.Application;
using Social.Core;
using Social.Infrastructure;
using System.Text.Json.Serialization;
using AspNetCoreRateLimit;

var builder = WebApplication.CreateBuilder(args);

// Load environment configuration
builder.AddEnvironmentConfiguration();

// Add Redis caching
builder.AddRedisCache();

// Add services to the container.  

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.IgnoreReadOnlyProperties = true;
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.Converters.Add(new Social.API.Serialization.UtcDateTimeJsonConverter());
    options.JsonSerializerOptions.Converters.Add(new Social.API.Serialization.NullableUtcDateTimeJsonConverter());
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi  
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "Social API";
        document.Info.Version = "v1";
        
        // Add security scheme
        document.Components ??= new();
        document.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();
        
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token."
        };
        
        // Add security requirement
        document.SecurityRequirements = new List<OpenApiSecurityRequirement>
        {
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                }] = Array.Empty<string>()
            }
        };
        
        return Task.CompletedTask;
    });
});

// dependency injection  
builder.Services.AddCoreDI();
builder.AddInfrastructureDI();
builder.Services.AddApplicationDI();
builder.Services.AddAdminWebUI();

builder.Services.AddTransient<AuthEndpoints>();

// Add rate limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost",
        builder =>
        {
            builder
                .WithOrigins([
                    "https://social.mohammed-aydan.site",
                    "https://dev-social.mohammed-aydan.site",
                    "https://mohammed-aydan.site",
                    "https://social-eg.vercel.app"
                     ])
                .WithOrigins(["http://localhost:3000", "http://localhost:8080", "http://localhost:5173", "http://localhost:5157/"])
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials(); // only if using cookies/auth
        });
});


var app = builder.Build();

// Configure the HTTP request pipeline.  
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Social API");
    });
}

//app.UseHttpsRedirection();


// Use CORS before any redirect
app.UseCors("AllowLocalhost");

app.UseStaticFiles();

// Global exception handling
app.UseMiddleware<GlobalExceptionMiddleware>();

// Rate limiting
app.UseIpRateLimiting();

app.UseAuthentication();
app.UseAuthorization();

// Token blacklist middleware
app.UseMiddleware<TokenBlacklistMiddleware>();

// app.UseWhen(context => context.Request.Path.StartsWithSegments("/api"), appBuilder =>
// {
//     appBuilder.UseMiddleware<AuthEndpoints>();
// });

app.MapControllers();

// Database & Role Seeding
if (!app.Environment.IsEnvironment("Testing"))
{
    try
    {
        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<Social.Core.Interfaces.IDatabaseSeeder>();
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning("Database seeding skipped or encountered an error: {Message}", ex.Message);
    }
}

app.Run();

public partial class Program { }
