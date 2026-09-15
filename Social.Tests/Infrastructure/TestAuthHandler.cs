using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Social.Tests.Infrastructure
{
    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string DefaultUserId = "test-user-id";
        public const string DefaultEmail = "test@example.com";
        public const string DefaultRole = "User";

        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Request.Cookies.TryGetValue("admin_token", out var cookieToken) && !string.IsNullOrWhiteSpace(cookieToken))
            {
                var cookieRole = cookieToken.Contains("non-admin") ? "User" : "Admin";
                var cookieClaims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "admin-cookie-user"),
                    new Claim(ClaimTypes.Name, "Admin Cookie User"),
                    new Claim(ClaimTypes.Email, "admin@social.com"),
                    new Claim(ClaimTypes.Role, cookieRole)
                };

                var cookieIdentity = new ClaimsIdentity(cookieClaims, "TestScheme");
                var cookiePrincipal = new ClaimsPrincipal(cookieIdentity);
                var cookieTicket = new AuthenticationTicket(cookiePrincipal, "TestScheme");

                return Task.FromResult(AuthenticateResult.Success(cookieTicket));
            }

            if (Request.Headers.TryGetValue("X-Anonymous", out var anonymous) && anonymous == "true")
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var userId = Request.Headers.TryGetValue("X-Test-UserId", out var customUserId)
                ? customUserId.ToString()
                : DefaultUserId;

            var role = Request.Headers.TryGetValue("X-Test-Role", out var customRole)
                ? customRole.ToString()
                : DefaultRole;

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.Email, DefaultEmail),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, "TestScheme");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "TestScheme");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
