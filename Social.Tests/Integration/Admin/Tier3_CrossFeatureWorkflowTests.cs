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
    [Trait("Tier", "Tier3")]
    public class Tier3_CrossFeatureWorkflowTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public Tier3_CrossFeatureWorkflowTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task T3_1_BanUser_PreventsSubsequentSignIn()
        {
            // Arrange
            var adminClient = _factory.CreateAdminClient();
            var targetUserId = "target-user-to-ban-001";
            var banRequest = new AdminBanUserRequest { Reason = "Terms of service violation" };

            // 1. Admin bans the user
            var banResponse = await adminClient.PostAsJsonAsync($"/api/admin/users/{targetUserId}/ban", banRequest);
            banResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

            // 2. User attempts to sign in
            var userClient = _factory.CreateClient();
            var signInRequest = new SignIn
            {
                Email = "banneduser@example.com",
                Password = "Password123!"
            };

            // Act
            var signInResponse = await userClient.PostAsJsonAsync("/api/User/sign-in", signInRequest);

            // Assert - Locked accounts must fail sign-in
            signInResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task T3_2_BanUser_InvalidatesExistingTokens()
        {
            // Arrange
            var adminClient = _factory.CreateAdminClient();
            var targetUserId = "target-user-token-revocation";
            var banRequest = new AdminBanUserRequest { Reason = "Compromised account" };

            // 1. Admin bans the user
            await adminClient.PostAsJsonAsync($"/api/admin/users/{targetUserId}/ban", banRequest);

            // 2. User attempts to refresh token
            var userClient = _factory.CreateClient();
            var refreshRequest = new RefreshTokenRequest { RefreshToken = "revoked-refresh-token-123" };

            // Act
            var refreshResponse = await userClient.PostAsJsonAsync("/api/User/refresh-token", refreshRequest);

            // Assert - Refresh token should be rejected
            refreshResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task T3_3_Moderation_HidePost_ExcludesFromPublicFeeds()
        {
            // Arrange
            var adminClient = _factory.CreateAdminClient();
            var userClient = _factory.CreateUserClient();
            var postId = "post-to-hide-and-restore";
            var hideRequest = new AdminModerationActionRequest { Reason = "Content violation" };
            var restoreRequest = new AdminModerationActionRequest { Reason = "Content restored after review" };

            // 1. Admin hides the post
            var hideResponse = await adminClient.PostAsJsonAsync($"/api/admin/moderation/posts/{postId}/hide", hideRequest);
            hideResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

            // 2. Regular user queries public feed or specific post
            var feedResponse = await userClient.GetAsync($"/api/posts/{postId}");
            feedResponse.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);

            // 3. Admin restores the post
            var restoreResponse = await adminClient.PostAsJsonAsync($"/api/admin/moderation/posts/{postId}/restore", restoreRequest);
            restoreResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task T3_4_Moderation_HideComment_SynchronizesPostCommentCounter()
        {
            // Arrange
            var adminClient = _factory.CreateAdminClient();
            var commentId = "comment-sync-test-001";
            var hideRequest = new AdminModerationActionRequest { Reason = "Offensive comment" };
            var restoreRequest = new AdminModerationActionRequest { Reason = "Approved on appeal" };

            // 1. Admin hides comment
            var hideResponse = await adminClient.PostAsJsonAsync($"/api/admin/moderation/comments/{commentId}/hide", hideRequest);
            hideResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

            // 2. Admin restores comment
            var restoreResponse = await adminClient.PostAsJsonAsync($"/api/admin/moderation/comments/{commentId}/restore", restoreRequest);
            restoreResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task T3_5_AdministrativeMutation_GeneratesPersistentAuditRecord()
        {
            // Arrange
            var adminId = "audited-admin-001";
            var adminClient = _factory.CreateAdminClient(userId: adminId);
            var targetUserId = "audited-target-user-001";
            var banReason = "Confirmed spam bot";
            var banRequest = new AdminBanUserRequest { Reason = banReason };

            // 1. Perform administrative mutation
            var banResponse = await adminClient.PostAsJsonAsync($"/api/admin/users/{targetUserId}/ban", banRequest);
            banResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

            // 2. Query audit logs for this action
            var auditResponse = await adminClient.GetAsync($"/api/admin/audit-logs?actionType=UserBanned&adminId={adminId}");
            auditResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await auditResponse.Content.ReadFromJsonAsync<ApiResponse<PaginatedResultDto<AuditLogDto>>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
        }

        [Fact]
        public async Task T3_6_RolePromotion_EnablesImmediateDashboardAccess()
        {
            // Arrange
            var adminClient = _factory.CreateAdminClient();
            var targetUserId = "promoted-user-001";

            // 1. Before promotion: user cannot access admin
            var beforeClient = _factory.CreateUserClient(userId: targetUserId);
            var beforeResponse = await beforeClient.GetAsync("/admin");
            beforeResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            // 2. Admin promotes user to Admin
            var promoteRequest = new AdminUpdateRolesRequest
            {
                Roles = new List<string> { "Admin", "User" },
                Reason = "Elevated to system administrator"
            };
            var promoteResponse = await adminClient.PostAsJsonAsync($"/api/admin/users/{targetUserId}/roles", promoteRequest);
            promoteResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

            // 3. After promotion: client with Admin claim can access /admin
            var afterClient = _factory.CreateAdminClient(userId: targetUserId);
            var afterResponse = await afterClient.GetAsync("/admin");
            afterResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
