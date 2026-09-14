using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Social.API.Middlewares;
using Social.Core.Common;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Social.Tests.Integration.Middleware
{
    public class GlobalExceptionMiddlewareTests
    {
        private readonly ILogger<GlobalExceptionMiddleware> _logger = Substitute.For<ILogger<GlobalExceptionMiddleware>>();

        [Fact]
        public async Task InvokeAsync_WhenArgumentExceptionThrown_ShouldReturn400BadRequest()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            RequestDelegate next = _ => throw new ArgumentException("Invalid argument provided");
            var middleware = new GlobalExceptionMiddleware(next, _logger);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
            var response = JsonSerializer.Deserialize<ApiResponse<object>>(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            response.Should().NotBeNull();
            response!.Success.Should().BeFalse();
            response.Message.Should().Be("Invalid argument provided");
        }

        [Fact]
        public async Task InvokeAsync_WhenKeyNotFoundExceptionThrown_ShouldReturn404NotFound()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            RequestDelegate next = _ => throw new KeyNotFoundException("Item not found");
            var middleware = new GlobalExceptionMiddleware(next, _logger);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
            var response = JsonSerializer.Deserialize<ApiResponse<object>>(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            response.Should().NotBeNull();
            response!.Success.Should().BeFalse();
            response.Message.Should().Be("Item not found");
        }

        [Fact]
        public async Task InvokeAsync_WhenUnauthorizedAccessExceptionThrown_ShouldReturn401Unauthorized()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            RequestDelegate next = _ => throw new UnauthorizedAccessException("Forbidden access");
            var middleware = new GlobalExceptionMiddleware(next, _logger);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
            var response = JsonSerializer.Deserialize<ApiResponse<object>>(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            response.Should().NotBeNull();
            response!.Success.Should().BeFalse();
            response.Message.Should().Be("Forbidden access");
        }

        [Fact]
        public async Task InvokeAsync_WhenUnhandledExceptionThrown_ShouldReturn500InternalServerError()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            RequestDelegate next = _ => throw new Exception("Critical failure");
            var middleware = new GlobalExceptionMiddleware(next, _logger);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
            var response = JsonSerializer.Deserialize<ApiResponse<object>>(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            response.Should().NotBeNull();
            response!.Success.Should().BeFalse();
            response.Message.Should().Be("An unexpected error occurred. Please try again later.");
        }
    }
}
