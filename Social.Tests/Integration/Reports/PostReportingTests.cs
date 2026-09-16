using FluentAssertions;
using Social.Application.Features.Reports.DTOs;
using Social.Core.Common;
using Social.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Social.Tests.Integration.Reports
{
    /// <summary>
    /// Worker-B (post-reporting T4/T5/T7): user report flow + admin triage flow.
    /// Uses the shared CustomWebApplicationFactory; posts resolve through the
    /// in-memory TestAdminRepository (unknown ids are dynamically seeded with
    /// author "target-user-001"; ids containing "non-existent" return null).
    /// Each test uses unique post/reporter ids: the report double is shared
    /// per test class, so tests must not overlap on (postId, reporterId).
    /// </summary>
    public class PostReportingTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public PostReportingTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static StringContent JsonBody(object value) =>
            new StringContent(
                System.Text.Json.JsonSerializer.Serialize(value),
                System.Text.Encoding.UTF8,
                "application/json");

        #region User report flow

        [Fact]
        public async Task ReportPost_ValidRequest_ReturnsOkWithDto()
        {
            var client = _factory.CreateUserClient("pr-reporter-001");
            var response = await client.PostAsJsonAsync(
                "/api/posts/pr-target-001/report",
                new ReportPostRequest { Reason = "Spam" });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<PostReportDto>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
            content.Data.Should().NotBeNull();
            content.Data!.PostId.Should().Be("pr-target-001");
            content.Data.ReporterUserId.Should().Be("pr-reporter-001");
            content.Data.Reason.Should().Be("Spam");
            content.Data.Status.Should().Be("Pending");
            content.Data.PostExcerpt.Should().NotBeNullOrWhiteSpace();
            content.Data.PostAuthorId.Should().Be("target-user-001");
        }

        [Fact]
        public async Task ReportPost_DuplicateOpen_ReturnsBadRequest()
        {
            var client = _factory.CreateUserClient("pr-reporter-002");
            var first = await client.PostAsJsonAsync(
                "/api/posts/pr-target-002/report",
                new ReportPostRequest { Reason = "Harassment" });
            first.StatusCode.Should().Be(HttpStatusCode.OK);

            var second = await client.PostAsJsonAsync(
                "/api/posts/pr-target-002/report",
                new ReportPostRequest { Reason = "Spam" });
            second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ReportPost_OwnPost_ReturnsBadRequest()
        {
            // post-001 is authored by target-user-001 in the test double.
            var client = _factory.CreateUserClient("target-user-001");
            var response = await client.PostAsJsonAsync(
                "/api/posts/post-001/report",
                new ReportPostRequest { Reason = "Spam" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ReportPost_MissingPost_ReturnsNotFound()
        {
            var client = _factory.CreateUserClient("pr-reporter-003");
            var response = await client.PostAsJsonAsync(
                "/api/posts/non-existent-post-xyz/report",
                new ReportPostRequest { Reason = "Spam" });

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ReportPost_DeletedPost_ReturnsNotFound()
        {
            // already-hidden-post is seeded IsDeleted=true.
            var client = _factory.CreateUserClient("pr-reporter-004");
            var response = await client.PostAsJsonAsync(
                "/api/posts/already-hidden-post/report",
                new ReportPostRequest { Reason = "Spam" });

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ReportPost_Anonymous_ReturnsUnauthorized()
        {
            var client = _factory.CreateAnonymousClient();
            var response = await client.PostAsJsonAsync(
                "/api/posts/pr-target-003/report",
                new ReportPostRequest { Reason = "Spam" });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ReportPost_InvalidReason_ReturnsBadRequest()
        {
            var client = _factory.CreateUserClient("pr-reporter-005");
            var response = await client.PostAsJsonAsync(
                "/api/posts/pr-target-004/report",
                new ReportPostRequest { Reason = "Bogus" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ReportPost_OtherWithoutDetails_ReturnsBadRequest()
        {
            var client = _factory.CreateUserClient("pr-reporter-006");
            var response = await client.PostAsJsonAsync(
                "/api/posts/pr-target-005/report",
                new ReportPostRequest { Reason = "Other" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ReportPost_OtherWithDetails_ReturnsOk()
        {
            var client = _factory.CreateUserClient("pr-reporter-007");
            var response = await client.PostAsJsonAsync(
                "/api/posts/pr-target-006/report",
                new ReportPostRequest { Reason = "Other", Details = "Custom policy concern" });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetMyReports_ReturnsPagedCallerReports()
        {
            var client = _factory.CreateUserClient("pr-reporter-008");
            await client.PostAsJsonAsync("/api/posts/pr-target-007/report",
                new ReportPostRequest { Reason = "Spam" });
            await client.PostAsJsonAsync("/api/posts/pr-target-008/report",
                new ReportPostRequest { Reason = "Violence" });

            var response = await client.GetAsync("/api/posts/reports/mine?Page=1&Limit=20");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<ReportsPageDto>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
            content.Data.Should().NotBeNull();
            content.Data!.TotalCount.Should().Be(2);
            content.Data.Items.Should().HaveCount(2);
            content.Data.Items.Should().OnlyContain(r => r.ReporterUserId == "pr-reporter-008");
        }

        [Fact]
        public async Task CancelReport_OwnPending_ReturnsOkAndRemoves()
        {
            var client = _factory.CreateUserClient("pr-reporter-009");
            var created = await client.PostAsJsonAsync("/api/posts/pr-target-009/report",
                new ReportPostRequest { Reason = "Nudity" });
            created.StatusCode.Should().Be(HttpStatusCode.OK);
            var createdContent = await created.Content.ReadFromJsonAsync<ApiResponse<PostReportDto>>();
            var reportId = createdContent!.Data!.Id;

            var cancel = await client.DeleteAsync($"/api/posts/reports/{reportId}");
            cancel.StatusCode.Should().Be(HttpStatusCode.OK);

            var mine = await client.GetAsync("/api/posts/reports/mine?Page=1&Limit=20");
            var mineContent = await mine.Content.ReadFromJsonAsync<ApiResponse<ReportsPageDto>>();
            mineContent!.Data!.Items.Should().NotContain(r => r.Id == reportId);
        }

        [Fact]
        public async Task CancelReport_ForeignReport_ReturnsUnauthorized()
        {
            var owner = _factory.CreateUserClient("pr-reporter-010");
            var created = await owner.PostAsJsonAsync("/api/posts/pr-target-010/report",
                new ReportPostRequest { Reason = "Spam" });
            var reportId = (await created.Content.ReadFromJsonAsync<ApiResponse<PostReportDto>>())!.Data!.Id;

            var stranger = _factory.CreateUserClient("pr-reporter-011");
            var cancel = await stranger.DeleteAsync($"/api/posts/reports/{reportId}");

            cancel.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task CancelReport_MissingReport_ReturnsNotFound()
        {
            var client = _factory.CreateUserClient("pr-reporter-012");
            var cancel = await client.DeleteAsync("/api/posts/reports/does-not-exist-001");

            cancel.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region Admin triage flow

        [Fact]
        public async Task AdminQueue_AsAdmin_ReturnsPagedWithExcerpt()
        {
            var reporter = _factory.CreateUserClient("pr-reporter-020");
            await reporter.PostAsJsonAsync("/api/posts/pr-target-020/report",
                new ReportPostRequest { Reason = "Misinformation" });

            var admin = _factory.CreateAdminClient();
            var response = await admin.GetAsync("/api/admin/moderation/reports?page=1&pageSize=20");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<ReportsPageDto>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
            content.Data.Should().NotBeNull();
            content.Data!.TotalCount.Should().BeGreaterThan(0);
            content.Data.Items.Should().Contain(r => r.PostId == "pr-target-020");
        }

        [Fact]
        public async Task AdminQueue_FilterByStatus_ReturnsOnlyMatching()
        {
            var admin = _factory.CreateAdminClient();
            var response = await admin.GetAsync("/api/admin/moderation/reports?status=Pending&page=1&pageSize=20");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<ReportsPageDto>>();
            content!.Data!.Items.Should().OnlyContain(r => r.Status == "Pending");
        }

        [Fact]
        public async Task AdminQueue_AsUser_ReturnsForbidden()
        {
            var client = _factory.CreateUserClient("pr-reporter-021");
            var response = await client.GetAsync("/api/admin/moderation/reports?page=1&pageSize=20");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task AdminResolve_Dismiss_UpdatesStatusAndWritesAudit()
        {
            var reporter = _factory.CreateUserClient("pr-reporter-022");
            var created = await reporter.PostAsJsonAsync("/api/posts/pr-target-022/report",
                new ReportPostRequest { Reason = "Copyright" });
            var reportId = (await created.Content.ReadFromJsonAsync<ApiResponse<PostReportDto>>())!.Data!.Id;

            var admin = _factory.CreateAdminClient();
            var resolve = await admin.PostAsJsonAsync(
                $"/api/admin/moderation/reports/{reportId}/resolve",
                new ResolveReportRequest { Action = "dismiss", Note = "Reviewed, no violation" });

            resolve.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await resolve.Content.ReadFromJsonAsync<ApiResponse<PostReportDto>>();
            content!.Data!.Status.Should().Be("Dismissed");

            var single = await admin.GetAsync($"/api/admin/moderation/reports/{reportId}");
            single.StatusCode.Should().Be(HttpStatusCode.OK);
            var singleContent = await single.Content.ReadFromJsonAsync<ApiResponse<PostReportDto>>();
            singleContent!.Data!.Status.Should().Be("Dismissed");

            var (logs, _) = await _factory.AuditLogRepositoryInstance.GetPagedAsync(1, 100, actionType: "ReportDismissed");
            logs.Should().Contain(l => l.TargetId == reportId);
        }

        [Fact]
        public async Task AdminResolve_HidePost_HidesPostAndMarksActioned()
        {
            var reporter = _factory.CreateUserClient("pr-reporter-023");
            var created = await reporter.PostAsJsonAsync("/api/posts/pr-target-023/report",
                new ReportPostRequest { Reason = "HateSpeech" });
            var reportId = (await created.Content.ReadFromJsonAsync<ApiResponse<PostReportDto>>())!.Data!.Id;

            var admin = _factory.CreateAdminClient();
            var resolve = await admin.PostAsJsonAsync(
                $"/api/admin/moderation/reports/{reportId}/resolve",
                new ResolveReportRequest { Action = "hide_post", Note = "Violates policy" });

            resolve.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await resolve.Content.ReadFromJsonAsync<ApiResponse<PostReportDto>>();
            content!.Data!.Status.Should().Be("Actioned");

            var post = await _factory.AdminRepositoryInstance.GetPostByIdAsync("pr-target-023");
            post.Should().NotBeNull();
            post!.IsDeleted.Should().BeTrue();

            var (logs, _) = await _factory.AuditLogRepositoryInstance.GetPagedAsync(1, 100, actionType: "ReportActioned");
            logs.Should().Contain(l => l.TargetId == reportId);
        }

        [Fact]
        public async Task AdminResolve_AlreadyResolved_ReturnsBadRequest()
        {
            var reporter = _factory.CreateUserClient("pr-reporter-024");
            var created = await reporter.PostAsJsonAsync("/api/posts/pr-target-024/report",
                new ReportPostRequest { Reason = "Spam" });
            var reportId = (await created.Content.ReadFromJsonAsync<ApiResponse<PostReportDto>>())!.Data!.Id;

            var admin = _factory.CreateAdminClient();
            var first = await admin.PostAsJsonAsync(
                $"/api/admin/moderation/reports/{reportId}/resolve",
                new ResolveReportRequest { Action = "dismiss" });
            first.StatusCode.Should().Be(HttpStatusCode.OK);

            var second = await admin.PostAsJsonAsync(
                $"/api/admin/moderation/reports/{reportId}/resolve",
                new ResolveReportRequest { Action = "dismiss" });
            second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task AdminResolve_MissingReport_ReturnsNotFound()
        {
            var admin = _factory.CreateAdminClient();
            var resolve = await admin.PostAsJsonAsync(
                "/api/admin/moderation/reports/does-not-exist-002/resolve",
                new ResolveReportRequest { Action = "dismiss" });

            resolve.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task AdminResolve_InvalidAction_ReturnsBadRequest()
        {
            var reporter = _factory.CreateUserClient("pr-reporter-025");
            var created = await reporter.PostAsJsonAsync("/api/posts/pr-target-025/report",
                new ReportPostRequest { Reason = "Spam" });
            var reportId = (await created.Content.ReadFromJsonAsync<ApiResponse<PostReportDto>>())!.Data!.Id;

            var admin = _factory.CreateAdminClient();
            var resolve = await admin.PostAsJsonAsync(
                $"/api/admin/moderation/reports/{reportId}/resolve",
                new ResolveReportRequest { Action = "nuke_from_orbit" });

            resolve.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CancelReport_ResolvedReport_ReturnsBadRequest()
        {
            var reporter = _factory.CreateUserClient("pr-reporter-026");
            var created = await reporter.PostAsJsonAsync("/api/posts/pr-target-026/report",
                new ReportPostRequest { Reason = "Spam" });
            var reportId = (await created.Content.ReadFromJsonAsync<ApiResponse<PostReportDto>>())!.Data!.Id;

            var admin = _factory.CreateAdminClient();
            await admin.PostAsJsonAsync(
                $"/api/admin/moderation/reports/{reportId}/resolve",
                new ResolveReportRequest { Action = "dismiss" });

            var cancel = await reporter.DeleteAsync($"/api/posts/reports/{reportId}");
            cancel.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion
    }
}
