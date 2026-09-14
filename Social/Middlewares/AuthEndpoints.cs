
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Social.API.Middlewares
{
    public class AuthEndpoints : IMiddleware
    {
        private readonly string _apiKey;
        private readonly ILogger<AuthEndpoints> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public AuthEndpoints(IConfiguration configuration, ILogger<AuthEndpoints> logger)
        {
            _logger = logger;

            var apiKey = configuration["ApiSettings:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogCritical("API Key is not configured. The application cannot authenticate requests.");
                throw new InvalidOperationException(
                    "API Key is not configured in the application settings. " +
                    "Please set 'API_KEY' in your .env file.");
            }

            _apiKey = apiKey;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var requestId = context.TraceIdentifier;
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var path = context.Request.Path.ToString();
            var method = context.Request.Method;

            // 1) Check if the header exists
            if (!context.Request.Headers.TryGetValue("x-api-key", out var extractedApiKey))
            {
                _logger.LogWarning(
                    "Request [{RequestId}] from {ClientIp} to {Method} {Path} — missing 'x-api-key' header.",
                    requestId, clientIp, method, path);

                await WriteErrorResponseAsync(context, 401, new ErrorResponse
                {
                    Status = 401,
                    Error = "Unauthorized",
                    Message = "Authentication required. The 'x-api-key' header is missing from the request.",
                    RequestId = requestId
                });
                return;
            }

            var apiKeyValue = extractedApiKey.ToString();

            // 2) Check if the header value is empty or whitespace
            if (string.IsNullOrWhiteSpace(apiKeyValue))
            {
                _logger.LogWarning(
                    "Request [{RequestId}] from {ClientIp} to {Method} {Path} — 'x-api-key' header is empty.",
                    requestId, clientIp, method, path);

                await WriteErrorResponseAsync(context, 401, new ErrorResponse
                {
                    Status = 401,
                    Error = "Unauthorized",
                    Message = "Authentication required. The 'x-api-key' header is present but has no value.",
                    RequestId = requestId
                });
                return;
            }

            // 3) Validate the key using constant-time comparison to prevent timing attacks
            if (!FixedTimeEquals(_apiKey, apiKeyValue))
            {
                _logger.LogWarning(
                    "Request [{RequestId}] from {ClientIp} to {Method} {Path} — invalid API key provided.",
                    requestId, clientIp, method, path);

                await WriteErrorResponseAsync(context, 403, new ErrorResponse
                {
                    Status = 403,
                    Error = "Forbidden",
                    Message = "Access denied. The provided API key is invalid or has been revoked.",
                    RequestId = requestId
                });
                return;
            }

            // 4) Key is valid — continue
            _logger.LogDebug(
                "Request [{RequestId}] from {ClientIp} to {Method} {Path} — authenticated successfully.",
                requestId, clientIp, method, path);

            await next(context);
        }

        /// <summary>
        /// Compares two strings in constant time to prevent timing-based side-channel attacks.
        /// </summary>
        private static bool FixedTimeEquals(string expected, string actual)
        {
            var expectedBytes = Encoding.UTF8.GetBytes(expected);
            var actualBytes = Encoding.UTF8.GetBytes(actual);
            return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
        }

        /// <summary>
        /// Writes a structured JSON error response with security headers.
        /// </summary>
        private static async Task WriteErrorResponseAsync(HttpContext context, int statusCode, ErrorResponse error)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json; charset=utf-8";

            // Security headers
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["Cache-Control"] = "no-store";

            await context.Response.WriteAsync(JsonSerializer.Serialize(error, _jsonOptions));
        }

        private sealed class ErrorResponse
        {
            public int Status { get; set; }
            public string Error { get; set; } = null!;
            public string Message { get; set; } = null!;
            public string RequestId { get; set; } = null!;
            public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");
        }
    }
}
