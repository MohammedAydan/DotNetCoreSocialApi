using MediatR;
using Social.Core.Entities;
using Social.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Social.Application.Features.Admin.Users.Commands
{
    public record UnbanUserCommand(
        string AdminId,
        string? AdminEmail,
        string TargetUserId,
        string Reason
    ) : IRequest<bool>;

    public class UnbanUserCommandHandler : IRequestHandler<UnbanUserCommand, bool>
    {
        private readonly IAdminRepository _adminRepo;
        private readonly IAuditLogRepository _auditRepo;
        private readonly ICacheService _cache;

        public UnbanUserCommandHandler(IAdminRepository adminRepo, IAuditLogRepository auditRepo, ICacheService cache)
        {
            _adminRepo = adminRepo;
            _auditRepo = auditRepo;
            _cache = cache;
        }

        public async Task<bool> Handle(UnbanUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _adminRepo.GetUserByIdAsync(request.TargetUserId, cancellationToken);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID '{request.TargetUserId}' was not found.");
            }

            if (!user.LockoutEnd.HasValue || user.LockoutEnd.Value <= DateTimeOffset.UtcNow)
            {
                throw new InvalidOperationException("User is not currently banned or locked out.");
            }

            var success = await _adminRepo.UnlockUserAsync(request.TargetUserId, cancellationToken);
            if (!success)
            {
                throw new InvalidOperationException("Failed to unban user.");
            }

            // Remove blacklist entry from cache
            await _cache.RemoveAsync($"blacklisted_user:{request.TargetUserId}");

            await _auditRepo.AddAsync(new AuditLog
            {
                AdminId = request.AdminId,
                AdminEmail = request.AdminEmail,
                ActionType = "UserUnbanned",
                TargetEntity = "User",
                TargetId = request.TargetUserId,
                Reason = request.Reason,
                TimestampUtc = DateTime.UtcNow
            }, cancellationToken);

            return true;
        }
    }
}
