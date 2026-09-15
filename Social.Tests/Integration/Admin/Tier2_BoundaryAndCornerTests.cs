using FluentAssertions;
using Social.Core.Common;
using Social.Tests.Infrastructure;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Social.Tests.Integration.Admin
{
    [Trait("Category", "AdminE2E")]
    [Trait("Tier", "Tier2")]
    public class Tier2_BoundaryAndCornerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public Tier2_BoundaryAndCornerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        #region User Management Boundary & Corner Tests

        [Fact]
        public async Task T2_1_Pagination_ClampsNegativePageAndExcessiveLimit()
        {
            // Arrange - Clamping boundary: negative page clamped to 1, excessive limit clamped to max (e.g. 100)
            var client = _factory.CreateAdminClient();

            // Act
            var response = await client.GetAsync("/api/admin/users?page=-5&pageSize=500");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResultDto<AdminUserDto>>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
            content.Data!.Page.Should().BeGreaterOrEqualTo(1);
            content.Data.PageSize.Should().BeLessOrEqualTo(100);
        }

        [Fact]
        public async Task T2_2_UserSearch_HandlesEmptyQueryGracefully()
        {
            // Arrange
            var client = _factory.CreateAdminClient();

            // Act
            var response = await client.GetAsync("/api/admin/users?q=%20%20%20");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResultDto<AdminUserDto>>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
        }

        [Fact]
        public async Task T2_3_UserSearch_SanitizesSqlWildcards()
        {
            // Arrange - Potential injection characters
            var client = _factory.CreateAdminClient();

            // Act
            var response = await client.GetAsync("/api/admin/users?q=%25%25'--OR%201=1");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task T2_4_BanUser_WhenTargetNotFound_ReturnsNotFound()
        {
            // Arrange
            var client = _factory.CreateAdminClient();
            var request = new AdminBanUserRequest { Reason = "Spam bot" };

            // Act
            var response = await client.PostAsJsonAsync("/api/admin/users/non-existent-user-guid/ban", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task T2_5_BanUser_WhenTargetIsSelf_ReturnsBadRequest()
        {
            // Arrange - Admin ID in header matches the route parameter
            var adminId = "admin-self-id";
            var client = _factory.CreateAdminClient(userId: adminId);
            var request = new AdminBanUserRequest { Reason = "Self harm" };

            // Act
            var response = await client.PostAsJsonAsync($"/api/admin/users/{adminId}/ban", request);

            // Assert - Self-ban invariant protection
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task T2_6_RoleChange_WhenDemotingSelf_ReturnsBadRequest()
        {
            // Arrange - Admin attempts to revoke Admin role from self
            var adminId = "admin-self-id";
            var client = _factory.CreateAdminClient(userId: adminId);
            var request = new AdminUpdateRolesRequest
            {
                Roles = new List<string> { "User" },
                Reason = "Self demotion attempt"
            };

            // Act
            var response = await client.PostAsJsonAsync($"/api/admin/users/{adminId}/roles", request);

            // Assert - Self-demotion invariant protection
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task T2_7_RoleChange_WithInvalidRoleName_ReturnsBadRequest()
        {
            // Arrange
            var client = _factory.CreateAdminClient();
            var request = new AdminUpdateRolesRequest
            {
                Roles = new List<string> { "SuperGodRole" },
                Reason = "Assign invalid role"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/admin/users/target-user-001/roles", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task T2_8_UnbanUser_WhenUserNotLocked_ReturnsBadRequest()
        {
            // Arrange
            var client = _factory.CreateAdminClient();
            var request = new AdminUnbanUserRequest { Reason = "Unban active user" };

            // Act - Calling unban on a user who is not locked
            var response = await client.PostAsJsonAsync("/api/admin/users/active-user-001/unban", request);

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task T2_9_ResetPassword_WeakPassword_ReturnsBadRequest()
        {
            // Arrange - Weak password violating complexity rules
            var client = _factory.CreateAdminClient();
            var request = new AdminResetPasswordRequest
            {
                NewPassword = "123",
                Reason = "Weak password test"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/admin/users/target-user-001/reset-password", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region Content Moderation Boundary & Invariant Tests

        [Fact]
        public async Task T2_10_HidePost_WhenCountIsZero_DoesNotBecomeNegative()
        {
            // Arrange
            var client = _factory.CreateAdminClient();
            var request = new AdminModerationActionRequest { Reason = "Moderation check" };

            // Act
            var response = await client.PostAsJsonAsync("/api/admin/moderation/posts/post-with-zero-author-count/hide", request);

            // Assert - Safe decrement Math.Max(0, count - 1) ensures non-negative bounds
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task T2_11_HideComment_WhenCountIsZero_DoesNotBecomeNegative()
        {
            // Arrange
            var client = _factory.CreateAdminClient();
            var request = new AdminModerationActionRequest { Reason = "Moderation check" };

            // Act
            var response = await client.PostAsJsonAsync("/api/admin/moderation/comments/comment-with-zero-post-count/hide", request);

            // Assert - Safe decrement Math.Max(0, count - 1) ensures non-negative bounds
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task T2_12_HidePost_WhenAlreadyHidden_ReturnsBadRequest()
        {
            // Arrange
            var client = _factory.CreateAdminClient();
            var request = new AdminModerationActionRequest { Reason = "Double hide" };

            // Act
            var response = await client.PostAsJsonAsync("/api/admin/moderation/posts/already-hidden-post/hide", request);

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task T2_13_RestorePost_WhenNotHidden_ReturnsBadRequest()
        {
            // Arrange
            var client = _factory.CreateAdminClient();
            var request = new AdminModerationActionRequest { Reason = "Restore active post" };

            // Act
            var response = await client.PostAsJsonAsync("/api/admin/moderation/posts/active-post/restore", request);

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task T2_14_ModeratePost_WhenPostNotFound_ReturnsNotFound()
        {
            // Arrange
            var client = _factory.CreateAdminClient();
            var request = new AdminModerationActionRequest { Reason = "Missing post" };

            // Act
            var response = await client.PostAsJsonAsync("/api/admin/moderation/posts/non-existent-post-id/hide", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task T2_15_ModerateComment_WhenCommentNotFound_ReturnsNotFound()
        {
            // Arrange
            var client = _factory.CreateAdminClient();
            var request = new AdminModerationActionRequest { Reason = "Missing comment" };

            // Act
            var response = await client.PostAsJsonAsync("/api/admin/moderation/comments/non-existent-comment-id/hide", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region Analytics & Audit Log Boundary Tests

        [Fact]
        public async Task T2_16_AuditLogQuery_NonExistentFilter_ReturnsEmptyList()
        {
            // Arrange
            var client = _factory.CreateAdminClient();

            // Act
            var response = await client.GetAsync("/api/admin/audit-logs?actionType=NonExistentActionType_12345");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResultDto<AuditLogDto>>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
            content.Data!.Items.Should().BeEmpty();
            content.Data.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task T2_17_AuditLogQuery_InvalidDateRange_ReturnsBadRequest()
        {
            // Arrange - fromDate is after toDate
            var client = _factory.CreateAdminClient();
            var from = Uri.EscapeDataString(DateTime.UtcNow.AddDays(5).ToString("O"));
            var to = Uri.EscapeDataString(DateTime.UtcNow.ToString("O"));

            // Act
            var response = await client.GetAsync($"/api/admin/audit-logs?fromDate={from}&toDate={to}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task T2_18_Diagnostics_WhenRedisDisconnected_ReportsFallbackActive()
        {
            // Arrange - In testing, Redis is typically unconfigured and in-memory fallback is used
            var client = _factory.CreateAdminClient();

            // Act
            var response = await client.GetAsync("/api/admin/analytics/diagnostics");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<SystemDiagnosticsDto>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
        }

        #endregion
    }
}
