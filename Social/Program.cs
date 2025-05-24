using Social.API.Middlewares;
using Social.Application;
using Social.Core;
using Social.Infrastucture;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

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
builder.AddInfrastructureDI();
builder.AddCoreDI();
builder.AddApplicationDI();

builder.Services.AddTransient<AuthEndpoints>();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost3000",
        builder =>
        {
            builder
                .WithOrigins(["https://social.mohammed-aydan.me"])
                //.WithOrigins(["http://localhost:3000", "http://localhost:8080", "http://localhost:5173"])
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
app.UseCors("AllowLocalhost3000");

app.UseAuthentication();
app.UseAuthorization();

app.UseWhen(context => context.Request.Path.StartsWithSegments("/api"), appBuilder =>
{
    appBuilder.UseMiddleware<AuthEndpoints>();
});

app.MapControllers();

app.Run();
