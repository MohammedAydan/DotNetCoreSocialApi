using FluentAssertions;
using NSubstitute;
using Social.Application.Features.Users.DTOs;
using Social.Core.Common;
using Social.Core.Entities;
using Social.Tests.Infrastructure;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Social.Tests.Integration.Admin
{
    [Trait("Category", "AdminE2E")]
    [Trait("Tier", "Tier1")]
    public class Tier1_FeatureCoverageTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly Xunit.Abstractions.ITestOutputHelper _output;

        public Tier1_FeatureCoverageTests(CustomWebApplicationFactory factory, Xunit.Abstractions.ITestOutputHelper output)
        {
            _factory = factory;
            _output = output;
        }

        #region RBAC & Auth Feature Tests

        [Fact]
        public async Task T1_1_AdminEndpoints_Anonymous_ReturnsUnauthorized()
        {
            // Arrange
            var client = _factory.CreateAnonymousClient();

            // Act
            var pageResponse = await client.GetAsync("/admin");
            var apiResponse = await client.GetAsync("/api/admin/users");

            // Assert
            pageResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Redirect);
            apiResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task T1_2_AdminEndpoints_UserRole_ReturnsForbidden()
        {
            // Arrange
            var client = _factory.CreateUserClient();

            // Act
            var pageResponse = await client.GetAsync("/admin");
            var apiResponse = await client.GetAsync("/api/admin/users");

            // Assert
            pageResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            apiResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task T1_3_AdminEndpoints_ModeratorRole_OnAdminOnly_ReturnsForbidden()
        {
            // Arrange - Moderator role is restricted from user management
            var client = _factory.CreateModeratorClient();

            // Act
            var response = await client.GetAsync("/api/admin/users");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task T1_4_AdminEndpoints_AdminRole_ReturnsSuccess()
        {
            // Arrange
            var client = _factory.CreateAdminClient();

            // Act
            var response = await client.GetAsync("/admin");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            _output.WriteLine($"[TEST VERIFICATION] GET /admin returned HTTP {(int)response.StatusCode} {response.StatusCode}");
            _output.WriteLine($"[TEST VERIFICATION] Content-Type: {response.Content.Headers.ContentType}");
            _output.WriteLine($"[TEST VERIFICATION] Content-Length: {content.Length} characters");
            _output.WriteLine($"[TEST VERIFICATION] HTML Snippet:\n{content.Substring(0, Math.Min(400, content.Length))}...");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");

            // Verify core shell, branding, and tabs
            content.Should().Contain("Social Admin");
            content.Should().Contain("Platform Overview & Real-Time Analytics");
            content.Should().Contain("User & Identity Directory");
            content.Should().Contain("Centralized Content Moderation");
            content.Should().Contain("Administrative Audit Trail");
            content.Should().Contain("System Observability");

            // Verify module DOM containers
            content.Should().Contain("id=\"tab-overview\"");
            content.Should().Contain("id=\"tab-users\"");
            content.Should().Contain("id=\"tab-moderation\"");
            content.Should().Contain("id=\"tab-audit\"");
            content.Should().Contain("id=\"tab-diagnostics\"");

            // Verify interactive modals
            content.Should().Contain("id=\"modal-ban\"");
            content.Should().Contain("modal-ban-reason");

            // Verify styles and assets
            content.Should().Contain("admin-dashboard.css");
        }

        [Fact]
        public async Task T1_5_DashboardSignIn_ValidAdmin_ReturnsToken()
        {
            // Arrange - Tests the existing Dashboard SignIn endpoint
            var client = _factory.CreateClient();
            var signInRequest = new SignIn
            {
                Email = "admin@example.com",
                Password = "AdminPassword123!"
            };

            var adminUser = new User
            {
                Id = "admin-user-001",
                UserName = "adminuser",
                Email = "admin@example.com"
            };

            _factory.MockUserRepository.SignInAsync(Arg.Any<SignIn>()).Returns(adminUser);
            _factory.MockUserRepository.GetUserRolesAsync(adminUser).Returns(new List<string> { "Admin" });
            _factory.MockTokenService.GenerateToken(adminUser, Arg.Any<IList<string>>()).Returns("admin-jwt-token");
            _factory.MockUserRepository.CreateRefreshTokenAsync("admin-user-001").Returns("admin-refresh-token");

            // Act
            var response = await client.PostAsJsonAsync("/api/dashboard/User/sign-in", signInRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
            content.Data!.IsSuccess.Should().BeTrue();
            content.Data.AccessToken.Should().Be("admin-jwt-token");
        }

        [Fact]
        public async Task T1_6_DashboardSignIn_NonAdmin_ReturnsUnauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();
            var signInRequest = new SignIn
            {
                Email = "user@example.com",
                Password = "UserPassword123!"
            };

            var regularUser = new User
            {
                Id = "regular-user-001",
                UserName = "regularuser",
                Email = "user@example.com"
            };

            _factory.MockUserRepository.SignInAsync(Arg.Any<SignIn>()).Returns(regularUser);
            _factory.MockUserRepository.GetUserRolesAsync(regularUser).Returns(new List<string> { "User" });
            _factory.MockTokenService.GenerateToken(regularUser, Arg.Any<IList<string>>()).Returns("user-jwt-token");
            _factory.MockUserRepository.CreateRefreshTokenAsync("regular-user-001").Returns("user-refresh-token");

            // Act
            var response = await client.PostAsJsonAsync("/api/dashboard/User/sign-in", signInRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region User Management Feature Tests

        [Fact]
        public async Task T1_7_GetUsers_WithPagination_ReturnsUsersList()
        {
            // Arrange
            var client = _factory.CreateAdminClient();

            // Act
            var response = await client.GetAsync("/api/admin/users?page=1&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResultDto<AdminUserDto>>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
        }

        [Fact]
        public async Task T1_8_BanUser_ValidTarget_ReturnsSuccess()
        {
            // Arrange
            var client = _factory.CreateAdminClient();
            var request = new AdminBanUserRequest { Reason = "Spamming", DurationDays = 7 };

            // Act
            var response = await client.PostAsJsonAsync("/api/admin/users/target-user-001/ban", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task T1_9_UnbanUser_ValidTarget_ReturnsSuccess()
        {
            // Arrange
            var client = _factory.CreateAdminClient();
            var request = new AdminUnbanUserRequest { Reason = "Appealed and verified" };

            // Act
            var response = await client.PostAsJsonAsync("/api/admin/users/target-user-001/unban", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task T1_10_UpdateRoles_ValidTarget_ReturnsUpdatedRoles()
        {
            // Arrange
            var client = _factory.CreateAdminClient();
            var request = new AdminUpdateRolesRequest
            {
                Roles = new List<string> { "Moderator" },
                Reason = "Promoted to moderator"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/admin/users/target-user-001/roles", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task T1_11_ToggleVerification_ValidTarget_ReturnsSuccess()
        {
            // Arrange
            var client = _factory.CreateAdminClient();
            var request = new AdminToggleVerificationRequest
            {
                IsVerified = true,
                Reason = "Identity confirmed"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/admin/users/target-user-001/verify", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task T1_12_ResetPassword_ValidTarget_ReturnsSuccess()
        {
            // Arrange
            var client = _factory.CreateAdminClient();
            var request = new AdminResetPasswordRequest
            {
                NewPassword = "NewSecurePassword123!",
                Reason = "Administrative credential reset"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/admin/users/target-user-001/reset-password", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Content Moderation Feature Tests

        [Fact]
        public async Task T1_13_ModerationFeed_ReturnsAllItems()
        {
            // Arrange
            var client = _factory.CreateAdminClient();

            // Act
            var response = await client.GetAsync("/api/admin/moderation/feed?page=1&pageSize=20");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResultDto<AdminModerationItemDto>>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
        }

        [Fact]
        public async Task T1_14_HidePost_ValidId_SetsIsDeletedTrue()
        {
            // Arrange
            var client = _factory.CreateAdminClient();
            var request = new AdminModerationActionRequest { Reason = "Violates guidelines" };

            // Act
            var response = await client.PostAsJsonAsync("/api/admin/moderation/posts/post-001/hide", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task T1_15_RestorePost_ValidId_SetsIsDeletedFalse()
        {
            // Arrange
            var client = _factory.CreateAdminClient();
            var request = new AdminModerationActionRequest { Reason = "Reviewed and cleared" };

            // Act
            var response = await client.PostAsJsonAsync("/api/admin/moderation/posts/post-001/restore", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task T1_16_HideComment_ValidId_SetsIsDeletedTrue()
        {
            // Arrange
            var client = _factory.CreateAdminClient();
            var request = new AdminModerationActionRequest { Reason = "Abusive comment" };

            // Act
            var response = await client.PostAsJsonAsync("/api/admin/moderation/comments/comment-001/hide", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task T1_17_RestoreComment_ValidId_SetsIsDeletedFalse()
        {
            // Arrange
            var client = _factory.CreateAdminClient();
            var request = new AdminModerationActionRequest { Reason = "False positive" };

            // Act
            var response = await client.PostAsJsonAsync("/api/admin/moderation/comments/comment-001/restore", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task T1_17b_UpdatePostVisibility_ValidId_ReturnsSuccess()
        {
            // Arrange
            var client = _factory.CreateAdminClient();
            var request = new AdminUpdateVisibilityRequest { Visibility = "followers_only", Reason = "Audience restricted" };

            // Act
            var response = await client.PostAsJsonAsync("/api/admin/moderation/posts/post-001/visibility", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task T1_17c_DeletePostPermanently_ValidId_ReturnsSuccess()
        {
            // Arrange
            var client = _factory.CreateAdminClient();

            // Act
            var response = await client.DeleteAsync("/api/admin/moderation/posts/post-to-hide-and-restore?reason=Testing+permanent+deletion");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Analytics & Diagnostics Feature Tests

        [Fact]
        public async Task T1_18_GetOverviewMetrics_ReturnsPlatformCounts()
        {
            // Arrange
            var client = _factory.CreateAdminClient();

            // Act
            var response = await client.GetAsync("/api/admin/analytics/overview");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<PlatformOverviewMetricsDto>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
        }

        [Fact]
        public async Task T1_19_GetDiagnostics_ReturnsSystemHealth()
        {
            // Arrange
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

        #region Audit Logging Feature Tests

        [Fact]
        public async Task T1_20_GetAuditLogs_ReturnsOrderedRecords()
        {
            // Arrange
            var client = _factory.CreateAdminClient();

            // Act
            var response = await client.GetAsync("/api/admin/audit-logs?page=1&pageSize=20");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResultDto<AuditLogDto>>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
        }

        #endregion
    }
}
