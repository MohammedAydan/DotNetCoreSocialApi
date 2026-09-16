using MediatR;
using Social.Application.Features.Reports.DTOs;
using Social.Core.Entities;
using Social.Core.Interfaces;
using Social.Core.Reporting;

namespace Social.Application.Features.Reports.Commands
{
    public record ResolveReportCommand(
        string ReportId,
        string AdminId,
        string? AdminEmail,
        string Action,
        string? Note
    ) : IRequest<PostReportDto>;

    public class ResolveReportCommandHandler : IRequestHandler<ResolveReportCommand, PostReportDto>
    {
        private const string ActionDismiss = "dismiss";
        private const string ActionHidePost = "hide_post";

        private readonly IPostReportRepository _reports;
        private readonly IAdminRepository _adminRepo;
        private readonly IAuditLogRepository _auditRepo;
        private readonly INotificationRepository? _notifRepo;

        public ResolveReportCommandHandler(
            IPostReportRepository reports,
            IAdminRepository adminRepo,
            IAuditLogRepository auditRepo,
            INotificationRepository? notifRepo = null)
        {
            _reports = reports;
            _adminRepo = adminRepo;
            _auditRepo = auditRepo;
            _notifRepo = notifRepo;
        }

        public async Task<PostReportDto> Handle(ResolveReportCommand request, CancellationToken cancellationToken)
        {
            var report = await _reports.GetByIdAsync(request.ReportId, cancellationToken)
                ?? throw new KeyNotFoundException($"Report '{request.ReportId}' was not found.");

            if (!string.Equals(report.Status, ReportStatuses.Pending, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only pending reports can be resolved.");

            var action = (request.Action ?? string.Empty).Trim().ToLowerInvariant();
            if (action != ActionDismiss && action != ActionHidePost)
                throw new ArgumentException($"Invalid resolve action '{request.Action}'. Expected 'dismiss' or 'hide_post'.", nameof(request.Action));

            var postContent = report.Post?.Content;
            var postAuthorId = report.Post?.UserId;

            string newStatus;
            string auditAction;
            if (action == ActionDismiss)
            {
                newStatus = ReportStatuses.Dismissed;
                auditAction = "ReportDismissed";
            }
            else
            {
                var post = await _adminRepo.GetPostByIdAsync(report.PostId, cancellationToken);
                if (post == null)
                    throw new KeyNotFoundException($"Post '{report.PostId}' was not found.");

                postContent = post.Content;
                postAuthorId = post.UserId;

                var hidden = await _adminRepo.HidePostAsync(report.PostId, cancellationToken);
                if (!hidden)
                    throw new InvalidOperationException("Failed to hide post.");

                newStatus = ReportStatuses.Actioned;
                auditAction = "ReportActioned";
            }

            var updated = await _reports.UpdateStatusAsync(
                request.ReportId, newStatus, request.AdminId, request.Note?.Trim(), cancellationToken);

            await _auditRepo.AddAsync(new AuditLog
            {
                AdminId = request.AdminId,
                AdminEmail = request.AdminEmail,
                ActionType = auditAction,
                TargetEntity = "PostReport",
                TargetId = request.ReportId,
                Reason = request.Note?.Trim(),
                TimestampUtc = DateTime.UtcNow
            }, cancellationToken);

            if (_notifRepo != null)
            {
                try
                {
                    await _notifRepo.AddAsync(new Notification
                    {
                        RecipientId = report.ReporterUserId,
                        UserId = request.AdminId,
                        Type = "ModerationNotice",
                        Message = action == ActionDismiss
                            ? "Thanks for your report. After review, no violation was found."
                            : "Thanks for your report. The reported post has been hidden after review.",
                        PostId = report.PostId,
                        CreatedAt = DateTime.UtcNow
                    }, cancellationToken);
                }
                catch
                {
                    // Non-blocking notification resilience
                }

                if (action == ActionHidePost && !string.IsNullOrEmpty(postAuthorId))
                {
                    try
                    {
                        await _notifRepo.AddAsync(new Notification
                        {
                            RecipientId = postAuthorId,
                            UserId = request.AdminId,
                            Type = "ModerationNotice",
                            Message = $"Your post has been hidden by administration. Reason: {request.Note?.Trim() ?? report.Reason}",
                            PostId = report.PostId,
                            CreatedAt = DateTime.UtcNow
                        }, cancellationToken);
                    }
                    catch
                    {
                        // Non-blocking notification resilience
                    }
                }
            }

            return ReportMapping.ToDto(updated, postContent, postAuthorId,
                await _reports.GetOpenCountForPostAsync(report.PostId, cancellationToken));
        }
    }
}
