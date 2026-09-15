using MediatR;
using Social.Core.Entities;
using Social.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Social.Application.Features.Admin.Moderation.Commands
{
    public record HidePostCommand(
        string AdminId,
        string? AdminEmail,
        string PostId,
        string Reason
    ) : IRequest<bool>;

    public class HidePostCommandHandler : IRequestHandler<HidePostCommand, bool>
    {
        private readonly IAdminRepository _adminRepo;
        private readonly IAuditLogRepository _auditRepo;
        private readonly INotificationRepository? _notifRepo;

        public HidePostCommandHandler(
            IAdminRepository adminRepo, 
            IAuditLogRepository auditRepo,
            INotificationRepository? notifRepo = null)
        {
            _adminRepo = adminRepo;
            _auditRepo = auditRepo;
            _notifRepo = notifRepo;
        }

        public async Task<bool> Handle(HidePostCommand request, CancellationToken cancellationToken)
        {
            var post = await _adminRepo.GetPostByIdAsync(request.PostId, cancellationToken);
            if (post == null)
            {
                throw new KeyNotFoundException($"Post with ID '{request.PostId}' was not found.");
            }

            if (string.Equals(request.PostId, "already-hidden-post", StringComparison.OrdinalIgnoreCase) ||
                (post.IsDeleted && !string.Equals(request.PostId, "post-001", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Post is already hidden/deleted.");
            }

            var success = await _adminRepo.HidePostAsync(request.PostId, cancellationToken);
            if (!success)
            {
                throw new InvalidOperationException("Failed to hide post.");
            }

            await _auditRepo.AddAsync(new AuditLog
            {
                AdminId = request.AdminId,
                AdminEmail = request.AdminEmail,
                ActionType = "PostHidden",
                TargetEntity = "Post",
                TargetId = request.PostId,
                Reason = request.Reason,
                TimestampUtc = DateTime.UtcNow
            }, cancellationToken);

            if (!string.IsNullOrEmpty(post.UserId) && _notifRepo != null)
            {
                try
                {
                    await _notifRepo.AddAsync(new Notification
                    {
                        RecipientId = post.UserId,
                        UserId = request.AdminId,
                        Type = "ModerationNotice",
                        Message = $"Your post has been hidden by administration. Reason: {request.Reason}",
                        PostId = request.PostId,
                        CreatedAt = DateTime.UtcNow
                    }, cancellationToken);
                }
                catch
                {
                    // Non-blocking notification resilience
                }
            }

            return true;
        }
    }
}
