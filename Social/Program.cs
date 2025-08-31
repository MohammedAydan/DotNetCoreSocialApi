using Social.API.Middlewares;
using Social.Application;
using Social.Core;
using Social.Infrastucture;
using System.Text.Json.Serialization;
using AspNetCoreRateLimit;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/social-api-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Add Serilog
builder.Host.UseSerilog();

// Add services to the container.  

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.IgnoreReadOnlyProperties = true;
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Add Rate Limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Learn more about configuring Swagger at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// dependency injection  
builder.AddCoreDI();
builder.AddInfrastructureDI();
builder.AddApplicationDI();

builder.Services.AddTransient<AuthEndpoints>();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost",
        builder =>
        {
            builder
                .WithOrigins(["https://social.mohammed-aydan.me", "https://dev-social.mohammed-aydan.me", "https://mohammed-aydan.me"])
                .WithOrigins(["http://localhost:3000", "http://localhost:8080", "http://localhost:5173"])
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials(); // only if using cookies/auth
        });
});


var app = builder.Build();

// Configure the HTTP request pipeline.  
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Social API v1");
    });
}

//app.UseHttpsRedirection();

// Add custom error handling middleware
app.UseMiddleware<ErrorHandlingMiddleware>();

// Use Rate Limiting
app.UseIpRateLimiting();

// Use CORS before any redirect
app.UseCors("AllowLocalhost");

app.UseAuthentication();
app.UseAuthorization();

app.UseWhen(context => context.Request.Path.StartsWithSegments("/api"), appBuilder =>
{
    appBuilder.UseMiddleware<AuthEndpoints>();
});

app.MapControllers();

app.Run();
