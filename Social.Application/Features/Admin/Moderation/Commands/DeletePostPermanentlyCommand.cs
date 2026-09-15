using MediatR;
using Social.Core.Entities;
using Social.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Social.Application.Features.Admin.Moderation.Commands
{
    public record DeletePostPermanentlyCommand(
        string AdminId,
        string? AdminEmail,
        string PostId,
        string Reason
    ) : IRequest<bool>;

    public class DeletePostPermanentlyCommandHandler : IRequestHandler<DeletePostPermanentlyCommand, bool>
    {
        private readonly IAdminRepository _adminRepo;
        private readonly IAuditLogRepository _auditRepo;
        private readonly INotificationRepository? _notifRepo;

        public DeletePostPermanentlyCommandHandler(
            IAdminRepository adminRepo, 
            IAuditLogRepository auditRepo,
            INotificationRepository? notifRepo = null)
        {
            _adminRepo = adminRepo;
            _auditRepo = auditRepo;
            _notifRepo = notifRepo;
        }

        public async Task<bool> Handle(DeletePostPermanentlyCommand request, CancellationToken cancellationToken)
        {
            var post = await _adminRepo.GetPostByIdAsync(request.PostId, cancellationToken);
            if (post == null)
            {
                throw new KeyNotFoundException($"Post with ID '{request.PostId}' was not found.");
            }

            var authorId = post.UserId;

            var success = await _adminRepo.DeletePostPermanentlyAsync(request.PostId, cancellationToken);
            if (!success)
            {
                throw new InvalidOperationException("Failed to permanently delete post.");
            }

            await _auditRepo.AddAsync(new AuditLog
            {
                AdminId = request.AdminId,
                AdminEmail = request.AdminEmail,
                ActionType = "PostPermanentlyDeleted",
                TargetEntity = "Post",
                TargetId = request.PostId,
                Reason = request.Reason,
                TimestampUtc = DateTime.UtcNow
            }, cancellationToken);

            if (!string.IsNullOrEmpty(authorId) && _notifRepo != null)
            {
                try
                {
                    await _notifRepo.AddAsync(new Notification
                    {
                        RecipientId = authorId,
                        UserId = request.AdminId,
                        Type = "ModerationNotice",
                        Message = $"Your post has been permanently removed by administration. Reason: {request.Reason}",
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
