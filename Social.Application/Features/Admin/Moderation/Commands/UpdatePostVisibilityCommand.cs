using MediatR;
using Social.Core.Entities;
using Social.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Social.Application.Features.Admin.Moderation.Commands
{
    public record UpdatePostVisibilityCommand(
        string AdminId,
        string? AdminEmail,
        string PostId,
        string Visibility,
        string Reason
    ) : IRequest<bool>;

    public class UpdatePostVisibilityCommandHandler : IRequestHandler<UpdatePostVisibilityCommand, bool>
    {
        private readonly IAdminRepository _adminRepo;
        private readonly IAuditLogRepository _auditRepo;
        private readonly INotificationRepository? _notifRepo;

        public UpdatePostVisibilityCommandHandler(
            IAdminRepository adminRepo, 
            IAuditLogRepository auditRepo,
            INotificationRepository? notifRepo = null)
        {
            _adminRepo = adminRepo;
            _auditRepo = auditRepo;
            _notifRepo = notifRepo;
        }

        public async Task<bool> Handle(UpdatePostVisibilityCommand request, CancellationToken cancellationToken)
        {
            var post = await _adminRepo.GetPostByIdAsync(request.PostId, cancellationToken);
            if (post == null)
            {
                throw new KeyNotFoundException($"Post with ID '{request.PostId}' was not found.");
            }

            var normalizedVisibility = request.Visibility?.Trim().ToLowerInvariant() ?? "public";
            if (normalizedVisibility != "public" && normalizedVisibility != "private" && normalizedVisibility != "followers_only")
            {
                normalizedVisibility = "public";
            }

            var success = await _adminRepo.UpdatePostVisibilityAsync(request.PostId, normalizedVisibility, cancellationToken);
            if (!success)
            {
                throw new InvalidOperationException("Failed to update post visibility.");
            }

            await _auditRepo.AddAsync(new AuditLog
            {
                AdminId = request.AdminId,
                AdminEmail = request.AdminEmail,
                ActionType = "PostVisibilityUpdated",
                TargetEntity = "Post",
                TargetId = request.PostId,
                Reason = $"{request.Reason} (Visibility set to: {normalizedVisibility})",
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
                        Message = $"The visibility of your post was changed to '{normalizedVisibility}' by administration. Reason: {request.Reason}",
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
