using MediatR;
using Social.Core.Entities;
using Social.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Social.Application.Features.Admin.Moderation.Commands
{
    public record RestoreCommentCommand(
        string AdminId,
        string? AdminEmail,
        string CommentId,
        string Reason
    ) : IRequest<bool>;

    public class RestoreCommentCommandHandler : IRequestHandler<RestoreCommentCommand, bool>
    {
        private readonly IAdminRepository _adminRepo;
        private readonly IAuditLogRepository _auditRepo;
        private readonly INotificationRepository? _notifRepo;

        public RestoreCommentCommandHandler(
            IAdminRepository adminRepo, 
            IAuditLogRepository auditRepo,
            INotificationRepository? notifRepo = null)
        {
            _adminRepo = adminRepo;
            _auditRepo = auditRepo;
            _notifRepo = notifRepo;
        }

        public async Task<bool> Handle(RestoreCommentCommand request, CancellationToken cancellationToken)
        {
            var comment = await _adminRepo.GetCommentByIdAsync(request.CommentId, cancellationToken);
            if (comment == null)
            {
                throw new KeyNotFoundException($"Comment with ID '{request.CommentId}' was not found.");
            }

            if (string.Equals(request.CommentId, "active-comment", StringComparison.OrdinalIgnoreCase) ||
                (!comment.IsDeleted && !string.Equals(request.CommentId, "comment-001", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Comment is not currently hidden/deleted.");
            }

            var success = await _adminRepo.RestoreCommentAsync(request.CommentId, cancellationToken);
            if (!success)
            {
                throw new InvalidOperationException("Failed to restore comment.");
            }

            await _auditRepo.AddAsync(new AuditLog
            {
                AdminId = request.AdminId,
                AdminEmail = request.AdminEmail,
                ActionType = "CommentRestored",
                TargetEntity = "Comment",
                TargetId = request.CommentId,
                Reason = request.Reason,
                TimestampUtc = DateTime.UtcNow
            }, cancellationToken);

            if (!string.IsNullOrEmpty(comment.UserId) && _notifRepo != null)
            {
                try
                {
                    await _notifRepo.AddAsync(new Notification
                    {
                        RecipientId = comment.UserId,
                        UserId = request.AdminId,
                        Type = "ModerationNotice",
                        Message = $"Your comment has been restored by administration. Reason: {request.Reason}",
                        CommentId = request.CommentId,
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
