using FluentAssertions;
using Social.Tests.Infrastructure;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Social.Tests.Integration.Admin
{
    public class ReportsDashboardRouteTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ReportsDashboardRouteTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetReportsPage_Anonymous_RedirectsToLoginPage()
        {
            // Arrange
            var client = _factory.CreateAnonymousClient();

            // Act
            var response = await client.GetAsync("/admin/reports");

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found);
            response.Headers.Location.Should().NotBeNull();
            response.Headers.Location!.ToString().Should().Be("/admin/login");
        }

        [Fact]
        public async Task GetReportsPage_WithAdminCookie_ReturnsOkAndRendersReportsQueue()
        {
            // Arrange
            var client = _factory.CreateAnonymousClient();
            client.DefaultRequestHeaders.Add("Cookie", "admin_token=valid-admin-jwt-token");

            // Act
            var response = await client.GetAsync("/admin/reports");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
            content.Should().Contain("Reported Posts");
            content.Should().Contain("Trust & Safety · Post Reports");
        }
    }
}
