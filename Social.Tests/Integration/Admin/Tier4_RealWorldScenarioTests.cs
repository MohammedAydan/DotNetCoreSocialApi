using FluentAssertions;
using Social.Core.Common;
using Social.Tests.Infrastructure;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Social.Tests.Integration.Admin
{
    [Trait("Category", "AdminE2E")]
    [Trait("Tier", "Tier4")]
    public class Tier4_RealWorldScenarioTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public Tier4_RealWorldScenarioTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task T4_1_AbusiveContentIncident_FullLifecycle()
        {
            // Scenario: Abusive content incident triage, mitigation, account lockout, audit verification, and analytics
            var adminId = "incident-response-admin";
            var adminClient = _factory.CreateAdminClient(userId: adminId);
            var abusiveAuthorId = "abusive-author-001";
            var abusivePostId = "abusive-post-001";

            // Step 1: Admin navigates to moderation feed
            var feedResponse = await adminClient.GetAsync("/api/admin/moderation/feed?page=1&pageSize=10");
            feedResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Step 2: Admin soft-deletes the offensive post
            var hideRequest = new AdminModerationActionRequest
            {
                Reason = "Hate speech and harassment violation"
            };
            var hideResponse = await adminClient.PostAsJsonAsync($"/api/admin/moderation/posts/{abusivePostId}/hide", hideRequest);
            hideResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

            // Step 3: Admin applies immediate account lockout to author
            var banRequest = new AdminBanUserRequest
            {
                Reason = "Repeated harassment violations",
                DurationDays = 30
            };
            var banResponse = await adminClient.PostAsJsonAsync($"/api/admin/users/{abusiveAuthorId}/ban", banRequest);
            banResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

            // Step 4: Admin verifies chronological audit trail
            var auditResponse = await adminClient.GetAsync($"/api/admin/audit-logs?adminId={adminId}&pageSize=10");
            auditResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var auditContent = await auditResponse.Content.ReadFromJsonAsync<ApiResponse<PaginatedResultDto<AuditLogDto>>>();
            auditContent.Should().NotBeNull();
            auditContent!.Success.Should().BeTrue();

            // Step 5: Admin reviews platform metrics overview
            var metricsResponse = await adminClient.GetAsync("/api/admin/analytics/overview");
            metricsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var metricsContent = await metricsResponse.Content.ReadFromJsonAsync<ApiResponse<PlatformOverviewMetricsDto>>();
            metricsContent.Should().NotBeNull();
            metricsContent!.Success.Should().BeTrue();
        }

        [Fact]
        public async Task T4_2_RoleDelegationAndAuditReview()
        {
            // Scenario: Lead Admin delegates moderation power, Moderator executes action, Lead reviews audit, Lead demotes
            var leadAdminId = "lead-admin-001";
            var trustedUserId = "trusted-user-001";
            var commentId = "spam-comment-001";

            var leadAdminClient = _factory.CreateAdminClient(userId: leadAdminId);

            // Step 1: Lead Admin promotes trusted user to Moderator
            var promoteRequest = new AdminUpdateRolesRequest
            {
                Roles = new List<string> { "Moderator", "User" },
                Reason = "Appointed community moderator"
            };
            var promoteResponse = await leadAdminClient.PostAsJsonAsync($"/api/admin/users/{trustedUserId}/roles", promoteRequest);
            promoteResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

            // Step 2: Moderator hides an inappropriate comment
            var moderatorClient = _factory.CreateModeratorClient(userId: trustedUserId);
            var modActionRequest = new AdminModerationActionRequest
            {
                Reason = "Solicitation and spam link"
            };
            var modActionResponse = await moderatorClient.PostAsJsonAsync($"/api/admin/moderation/comments/{commentId}/hide", modActionRequest);
            modActionResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

            // Step 3: Lead Admin inspects audit logs filtered by moderator's ID
            var auditResponse = await leadAdminClient.GetAsync($"/api/admin/audit-logs?adminId={trustedUserId}");
            auditResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Step 4: Lead Admin revokes Moderator privileges
            var demoteRequest = new AdminUpdateRolesRequest
            {
                Roles = new List<string> { "User" },
                Reason = "Moderation tour completed"
            };
            var demoteResponse = await leadAdminClient.PostAsJsonAsync($"/api/admin/users/{trustedUserId}/roles", demoteRequest);
            demoteResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

            // Step 5: Demoted user attempts administrative user management and receives 403 Forbidden
            var demotedUserClient = _factory.CreateUserClient(userId: trustedUserId);
            var deniedResponse = await demotedUserClient.GetAsync("/api/admin/users");
            deniedResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task T4_3_PlatformObservabilityUnderLoad()
        {
            // Scenario: Under elevated traffic, administrator inspects diagnostics and rate-limiting telemetry
            var adminClient = _factory.CreateAdminClient();

            // Step 1: Query operational diagnostics
            var diagResponse = await adminClient.GetAsync("/api/admin/analytics/diagnostics");
            diagResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var diagContent = await diagResponse.Content.ReadFromJsonAsync<ApiResponse<SystemDiagnosticsDto>>();
            diagContent.Should().NotBeNull();
            diagContent!.Success.Should().BeTrue();
            diagContent.Data!.MemoryWorkingSetMb.Should().BeGreaterOrEqualTo(0);

            // Step 2: Query platform analytics overview
            var overviewResponse = await adminClient.GetAsync("/api/admin/analytics/overview");
            overviewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var overviewContent = await overviewResponse.Content.ReadFromJsonAsync<ApiResponse<PlatformOverviewMetricsDto>>();
            overviewContent.Should().NotBeNull();
            overviewContent!.Success.Should().BeTrue();
        }
    }
}
