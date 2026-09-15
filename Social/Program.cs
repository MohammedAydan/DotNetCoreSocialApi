using DotNetEnv;
using Microsoft.OpenApi.Any;
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
        
        // Strip meaningless `default: null` annotations that ASP.NET emits for
        // optional parameters (Microsoft.OpenApi.Any.OpenApiNull). Generators
        // (Orval zod `.default(null)`, Dart defaults) cannot consume a null
        // default on a non-nullable schema, so the contract omits them.
        // Visited set guards recursive schemas.
        var visited = new HashSet<OpenApiSchema>();
        static void StripNullDefaults(OpenApiSchema? schema, HashSet<OpenApiSchema> visited)
        {
            if (schema is null || !visited.Add(schema))
                return;
            if (schema.Default is OpenApiNull)
            {
                schema.Default = null;
            }
            foreach (var prop in schema.Properties.Values)
                StripNullDefaults(prop, visited);
            StripNullDefaults(schema.Items, visited);
            StripNullDefaults(schema.AdditionalProperties, visited);
            StripNullDefaults(schema.Not, visited);
            foreach (var sub in schema.AllOf) StripNullDefaults(sub, visited);
            foreach (var sub in schema.AnyOf) StripNullDefaults(sub, visited);
            foreach (var sub in schema.OneOf) StripNullDefaults(sub, visited);
        }

        if (document.Paths is not null)
        {
            foreach (var path in document.Paths.Values)
            {
                foreach (var operation in path.Operations.Values)
                {
                    if (operation.Parameters is not null)
                    {
                        foreach (var parameter in operation.Parameters.OfType<OpenApiParameter>())
                            StripNullDefaults(parameter.Schema, visited);
                    }
                    if (operation.RequestBody is OpenApiRequestBody body)
                        foreach (var media in body.Content.Values)
                            StripNullDefaults(media.Schema, visited);
                    foreach (var response in operation.Responses.Values.OfType<OpenApiResponse>())
                        foreach (var media in response.Content.Values)
                            StripNullDefaults(media.Schema, visited);
                }
            }
        }
        if (document.Components?.Schemas is not null)
        {
            foreach (var schema in document.Components.Schemas.Values)
                StripNullDefaults(schema, visited);
        }

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
// OpenAPI document is always mapped so SDK generation (`pnpm run openapi:export`)
// works in every environment; the interactive Swagger UI stays dev-only.
app.MapOpenApi();
if (app.Environment.IsDevelopment())
{
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

// Backend-only request telemetry (Channel + 5s batch flush; never blocks responses)
app.UseMiddleware<RequestTelemetryMiddleware>();

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
