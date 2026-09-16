using FluentAssertions;
using Social.Tests.Infrastructure;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Social.Tests.Integration.Admin
{
    public class AdminDocsRouteTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public AdminDocsRouteTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetDocsPage_Anonymous_RedirectsToLoginPage()
        {
            // Arrange
            var client = _factory.CreateAnonymousClient();

            // Act
            var response = await client.GetAsync("/admin/docs");

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found);
            response.Headers.Location.Should().NotBeNull();
            response.Headers.Location!.ToString().Should().Be("/admin/login");
        }

        [Fact]
        public async Task GetDocsPage_WithNonAdminCookie_ReturnsForbidden()
        {
            // Arrange
            var client = _factory.CreateAnonymousClient();
            client.DefaultRequestHeaders.Add("Cookie", "admin_token=non-admin-token");

            // Act
            var response = await client.GetAsync("/admin/docs");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetDocsPage_WithAdminCookie_ReturnsOkAndRendersDocumentation()
        {
            // Arrange
            var client = _factory.CreateAnonymousClient();
            client.DefaultRequestHeaders.Add("Cookie", "admin_token=valid-admin-jwt-token");

            // Act
            var response = await client.GetAsync("/admin/docs");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
            content.Should().Contain("tab-docs");
            content.Should().Contain("Documentation");
            content.Should().Contain("/admin/docs");
        }

        [Fact]
        public async Task GetAdminPage_WithAdminCookie_StillReturnsOk()
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
        }
    }
}
