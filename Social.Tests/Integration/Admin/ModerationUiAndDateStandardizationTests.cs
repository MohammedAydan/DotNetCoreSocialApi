using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Social.Tests.Infrastructure;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace Social.Tests.Integration.Admin
{
    public class ModerationUiAndDateStandardizationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ModerationUiAndDateStandardizationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetModerationPage_WithAdminCookie_RendersElevatedUiComponents()
        {
            // Arrange
            var client = _factory.CreateAnonymousClient();
            client.DefaultRequestHeaders.Add("Cookie", "admin_token=valid-admin-jwt-token");

            // Act
            var response = await client.GetAsync("/admin/moderation");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");

            // View Switcher
            content.Should().Contain("view-switcher");
            content.Should().Contain("view-btn-stream");
            content.Should().Contain("view-btn-grid");
            content.Should().Contain("view-btn-list");

            // Stat Pills Bar
            content.Should().Contain("mod-pills-bar");
            content.Should().Contain("pill-all");
            content.Should().Contain("pill-posts");
            content.Should().Contain("pill-comments");
            content.Should().Contain("pill-media");
            content.Should().Contain("pill-hidden");

            // Elevated Stream Container
            content.Should().Contain("moderation-stream");

            // JavaScript Functions for Date and Presentation Elevation
            content.Should().Contain("function formatStandardDate(isoDateString)");
            content.Should().Contain("function renderPostContent(rawContent, id)");
            content.Should().Contain("function toggleTextExpand(id)");
            content.Should().Contain("function setModViewMode(mode)");
            content.Should().Contain("function selectModPill(pillKey)");
        }

        [Fact]
        public async Task GetUsersPage_WithAdminCookie_RendersJoinedHeaderAndDateFormatter()
        {
            // Arrange
            var client = _factory.CreateAnonymousClient();
            client.DefaultRequestHeaders.Add("Cookie", "admin_token=valid-admin-jwt-token");

            // Act
            var response = await client.GetAsync("/admin/users");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            content.Should().Contain("<th>Joined</th>");
            content.Should().Contain("formatStandardDate(u.createdAt)");
        }

        [Fact]
        public async Task UsersApi_CreatedAt_SerializesStrictIso8601WithUtcIndicatorZ()
        {
            // Arrange
            var client = _factory.CreateAdminClient();

            // Act
            var response = await client.GetAsync("/api/admin/users?page=1&pageSize=5");
            var rawJson = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            // Verify date serialization in JSON has trailing 'Z'
            var isoUtcRegex = new Regex(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?Z");
            isoUtcRegex.IsMatch(rawJson).Should().BeTrue("all timestamps serialized across the API must explicitly end in 'Z'");
        }

        [Fact]
        public async Task ModerationFeedApi_CreatedAt_SerializesStrictIso8601WithUtcIndicatorZ()
        {
            // Arrange
            var client = _factory.CreateAdminClient();

            // Act
            var response = await client.GetAsync("/api/admin/moderation/feed?page=1&pageSize=5");
            var rawJson = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var isoUtcRegex = new Regex(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?Z");
            isoUtcRegex.IsMatch(rawJson).Should().BeTrue("all timestamps serialized across the API must explicitly end in 'Z'");
        }

        [Fact]
        public async Task AuditLogsApi_TimestampUtc_SerializesStrictIso8601WithUtcIndicatorZ()
        {
            // Arrange
            var client = _factory.CreateAdminClient();

            // Trigger an administrative mutation to ensure an audit entry exists in the repository
            await client.PostAsJsonAsync("/api/admin/moderation/posts/post-001/visibility", new AdminUpdateVisibilityRequest
            {
                Visibility = "private",
                Reason = "UTC test audit record"
            });

            // Act
            var response = await client.GetAsync("/api/admin/audit-logs?page=1&pageSize=5");
            var rawJson = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var isoUtcRegex = new Regex(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?Z");
            isoUtcRegex.IsMatch(rawJson).Should().BeTrue("all timestamps serialized across the API must explicitly end in 'Z'");
        }
    }
}
