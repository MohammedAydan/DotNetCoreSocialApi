using DotNetEnv;
using Social.API.Middlewares;
using Social.Application;
using Social.Core;
using Social.Infrastucture;
using System.Text.Json.Serialization;

// Load .env file
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Map environment variables to IConfiguration sections
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    // Database
    ["ConnectionStrings:DefaultConnection"] = Environment.GetEnvironmentVariable("CONNECTION_STRING"),

    // JWT
    ["Jwt:Key"] = Environment.GetEnvironmentVariable("JWT_KEY"),
    ["Jwt:Issuer"] = Environment.GetEnvironmentVariable("JWT_ISSUER"),
    ["Jwt:Audience"] = Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
    ["Jwt:ExpireTime"] = Environment.GetEnvironmentVariable("JWT_EXPIRE_TIME"),

    // API Settings
    ["ApiSettings:ApiKey"] = Environment.GetEnvironmentVariable("API_KEY"),

    // General Config
    ["GenralConfig:FrontendUrl"] = Environment.GetEnvironmentVariable("FRONTEND_URL"),

    // Email Settings
    ["EmailSettings:SmtpServer"] = Environment.GetEnvironmentVariable("EMAIL_SMTP_SERVER"),
    ["EmailSettings:SmtpPort"] = Environment.GetEnvironmentVariable("EMAIL_SMTP_PORT"),
    ["EmailSettings:SenderName"] = Environment.GetEnvironmentVariable("EMAIL_SENDER_NAME"),
    ["EmailSettings:SenderEmail"] = Environment.GetEnvironmentVariable("EMAIL_SENDER_EMAIL"),
    ["EmailSettings:Username"] = Environment.GetEnvironmentVariable("EMAIL_USERNAME"),
    ["EmailSettings:Password"] = Environment.GetEnvironmentVariable("EMAIL_PASSWORD"),
    ["EmailSettings:EnableSSL"] = Environment.GetEnvironmentVariable("EMAIL_ENABLE_SSL"),
});

// Add services to the container.  

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.IgnoreReadOnlyProperties = true;
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi  
builder.Services.AddOpenApi();

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
                .WithOrigins([
                    "https://social.mohammed-aydan.me",
                    "https://dev-social.mohammed-aydan.me",
                    "https://mohammed-aydan.me",
                    "https://social-eg.vercel.app"
                     ])
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
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Social API");
    });
}

//app.UseHttpsRedirection();


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
