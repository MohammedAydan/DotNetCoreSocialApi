using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using Social.Admin.Web.Models;
using Social.Core.Entities;
using Social.Tests.Infrastructure;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Social.Tests.Integration.Admin
{
    public class AdminAuthenticationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public AdminAuthenticationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetAdminPage_Anonymous_RedirectsToLoginPage()
        {
            // Arrange
            var client = _factory.CreateAnonymousClient();

            // Act
            var response = await client.GetAsync("/admin");

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found);
            response.Headers.Location.Should().NotBeNull();
            response.Headers.Location!.ToString().Should().Be("/admin/login");
        }

        [Fact]
        public async Task GetLoginPage_ReturnsHtmlWithLoginForm()
        {
            // Arrange
            var client = _factory.CreateAnonymousClient();

            // Act
            var response = await client.GetAsync("/admin/login");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
            content.Should().Contain("Social Admin Console");
            content.Should().Contain("type=\"email\"");
            content.Should().Contain("type=\"password\"");
            content.Should().Contain("Sign In to Admin Console");
        }

        [Fact]
        public async Task GetLoginPage_AlreadyAuthenticatedAsAdmin_RedirectsToDashboard()
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
            client.DefaultRequestHeaders.Add("X-Test-UserId", "admin-123");

            // Act
            var response = await client.GetAsync("/admin/login");

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found);
            response.Headers.Location.Should().NotBeNull();
            response.Headers.Location!.ToString().Should().Be("/admin");
        }

        [Fact]
        public async Task PostLogin_EmptyCredentials_ReturnsBadRequest()
        {
            // Arrange
            var client = _factory.CreateAnonymousClient();
            var emptyRequest = new AdminLoginRequest { Email = "", Password = "" };

            // Act
            var response = await client.PostAsJsonAsync("/admin/login", emptyRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetAdminPage_WithAdminCookie_ReturnsOkAndRendersDashboard()
        {
            // Arrange
            var client = _factory.CreateAnonymousClient();
            client.DefaultRequestHeaders.Add("Cookie", "admin_token=valid-admin-jwt-token");

            // Act
            var response = await client.GetAsync("/admin");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
            content.Should().Contain("Social Admin");
            content.Should().Contain("Platform Overview & Real-Time Analytics");
        }

        [Fact]
        public async Task GetAdminPage_WithNonAdminCookie_ReturnsForbidden()
        {
            // Arrange
            var client = _factory.CreateAnonymousClient();
            client.DefaultRequestHeaders.Add("Cookie", "admin_token=non-admin-token");

            // Act
            var response = await client.GetAsync("/admin");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Logout_ClearsCookieAndRedirectsToLogin()
        {
            // Arrange
            var client = _factory.CreateAnonymousClient();

            // Act
            var response = await client.GetAsync("/admin/logout");

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found);
            response.Headers.Location.Should().NotBeNull();
            response.Headers.Location!.ToString().Should().Be("/admin/login");

            // Verify Set-Cookie header attempts to expire admin_token
            response.Headers.TryGetValues("Set-Cookie", out var cookieHeaders).Should().BeTrue();
            cookieHeaders.Should().Contain(c => c.Contains("admin_token"));
        }

        [Theory]
        [InlineData("/admin/users", "User & Identity Directory")]
        [InlineData("/admin/moderation", "Centralized Content Moderation")]
        [InlineData("/admin/audit-logs", "Administrative Audit Trail")]
        [InlineData("/admin/diagnostics", "System Observability & API Health")]
        public async Task GetDedicatedAdminRoutes_WithAdminCookie_ReturnsOkAndRendersSpecificPage(string route, string expectedTitle)
        {
            // Arrange
            var client = _factory.CreateAnonymousClient();
            client.DefaultRequestHeaders.Add("Cookie", "admin_token=valid-admin-jwt-token");

            // Act
            var response = await client.GetAsync(route);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
            content.Should().Contain(expectedTitle);
        }

        [Theory]
        [InlineData("/admin/users")]
        [InlineData("/admin/moderation")]
        [InlineData("/admin/audit-logs")]
        [InlineData("/admin/diagnostics")]
        public async Task GetDedicatedAdminRoutes_Anonymous_RedirectsToLogin(string route)
        {
            // Arrange
            var client = _factory.CreateAnonymousClient();

            // Act
            var response = await client.GetAsync(route);

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found);
            response.Headers.Location.Should().NotBeNull();
            response.Headers.Location!.ToString().Should().Be("/admin/login");
        }
    }
}
