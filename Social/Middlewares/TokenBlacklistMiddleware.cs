using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Social.Core.Interfaces;

namespace Social.API.Middlewares
{
    public class TokenBlacklistMiddleware
    {
        private readonly RequestDelegate _next;

        public TokenBlacklistMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ITokenService tokenService, ICacheService cache)
        {
            var token = string.Empty;
            var authorizationHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authorizationHeader) && authorizationHeader.StartsWith("Bearer "))
            {
                token = authorizationHeader.Substring("Bearer ".Length).Trim();
            }
            else if (context.Request.Cookies.TryGetValue("admin_token", out var cookieToken) && !string.IsNullOrWhiteSpace(cookieToken))
            {
                token = cookieToken;
            }

            if (!string.IsNullOrEmpty(token))
            {
                var isBlacklisted = await tokenService.IsTokenBlacklistedAsync(token);
                if (isBlacklisted)
                {
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"error\":\"Token has been revoked\"}");
                    return;
                }
            }

            // Platform-wide ban: reject live JWTs whose owner is blacklisted.
            // Runs after UseAuthentication/UseAuthorization so User claims are populated.
            var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!string.IsNullOrWhiteSpace(userId) && await cache.ExistsAsync($"blacklisted_user:{userId}"))
            {
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"error\":\"User account has been banned\"}");
                return;
            }

            await _next(context);
        }
    }
}