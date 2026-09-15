using MediatR;
using Social.Core.Entities;
using Social.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Social.Application.Features.Admin.Users.Commands
{
    public record BanUserCommand(
        string AdminId,
        string? AdminEmail,
        string TargetUserId,
        string Reason,
        int? DurationDays
    ) : IRequest<bool>;

    public class BanUserCommandHandler : IRequestHandler<BanUserCommand, bool>
    {
        private readonly IAdminRepository _adminRepo;
        private readonly IAuditLogRepository _auditRepo;
        private readonly ICacheService _cache;

        public BanUserCommandHandler(IAdminRepository adminRepo, IAuditLogRepository auditRepo, ICacheService cache)
        {
            _adminRepo = adminRepo;
            _auditRepo = auditRepo;
            _cache = cache;
        }

        public async Task<bool> Handle(BanUserCommand request, CancellationToken cancellationToken)
        {
            if (string.Equals(request.AdminId, request.TargetUserId, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Administrators cannot ban their own account.");
            }

            var user = await _adminRepo.GetUserByIdAsync(request.TargetUserId, cancellationToken);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID '{request.TargetUserId}' was not found.");
            }

            var success = await _adminRepo.LockUserAsync(request.TargetUserId, request.DurationDays, request.Reason, cancellationToken);
            if (!success)
            {
                throw new InvalidOperationException("Failed to ban user.");
            }

            // Immediately blacklist user in distributed cache for instantaneous token revocation
            await _cache.SetAsync($"blacklisted_user:{request.TargetUserId}", true, TimeSpan.FromDays(request.DurationDays ?? 365));

            await _auditRepo.AddAsync(new AuditLog
            {
                AdminId = request.AdminId,
                AdminEmail = request.AdminEmail,
                ActionType = "UserBanned",
                TargetEntity = "User",
                TargetId = request.TargetUserId,
                Reason = request.Reason,
                TimestampUtc = DateTime.UtcNow
            }, cancellationToken);

            return true;
        }
    }
}
