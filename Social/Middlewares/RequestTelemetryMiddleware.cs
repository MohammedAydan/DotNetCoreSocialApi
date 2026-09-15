using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Social.Core.Entities;
using Social.Core.Interfaces;

namespace Social.API.Middlewares
{
    /// <summary>
    /// High-throughput request telemetry. Times every <c>/api</c> request with
    /// <see cref="Stopwatch.GetTimestamp"/> and streams a <see cref="RequestLog"/>
    /// into the bounded channel without blocking the response pipeline.
    /// Excludes health checks, static assets, swagger/OpenAPI, and preflights.
    /// </summary>
    public class RequestTelemetryMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestTelemetryMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IRequestLogSink sink)
        {
            if (!ShouldCapture(context.Request))
            {
                await _next(context);
                return;
            }

            var start = Stopwatch.GetTimestamp();
            try
            {
                await _next(context);
            }
            finally
            {
                try
                {
                    var elapsedMs = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
                    sink.TryWrite(new RequestLog
                    {
                        UserId = Truncate(GetUserId(context.User), 255),
                        Endpoint = Truncate(context.Request.Path.Value ?? "/", 255),
                        HttpMethod = Truncate(context.Request.Method, 10),
                        StatusCode = context.Response.StatusCode,
                        DurationMs = elapsedMs,
                        IpAddress = Truncate(context.Connection.RemoteIpAddress?.ToString(), 45),
                        UserAgent = Truncate(context.Request.Headers.UserAgent.FirstOrDefault(), 512),
                        CreatedAt = DateTime.UtcNow
                    });
                }
                catch
                {
                    // Telemetry must never break a user request.
                }
            }
        }

        public static bool ShouldCapture(HttpRequest request)
        {
            if (HttpMethods.IsOptions(request.Method))
                return false;

            var path = request.Path.Value ?? string.Empty;

            // Only API traffic is metered; admin HTML shell, static RCL assets,
            // OpenAPI/swagger UI, and health probes are excluded.
            if (!path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
                return false;

            if (path.StartsWith("/api/health", StringComparison.OrdinalIgnoreCase))
                return false;

            if (path.Contains("swagger", StringComparison.OrdinalIgnoreCase) ||
                path.Contains("openapi", StringComparison.OrdinalIgnoreCase))
                return false;

            // Static-asset hits that slipped under /api (file extensions).
            var lastSegment = path[(path.LastIndexOf('/') + 1)..];
            if (lastSegment.Contains('.'))
                return false;

            return true;
        }

        private static string? GetUserId(ClaimsPrincipal? user)
        {
            var id = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return string.IsNullOrWhiteSpace(id) ? null : id;
        }

        private static string Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return value ?? string.Empty;
            return value.Length <= maxLength ? value : value[..maxLength];
        }
    }
}
