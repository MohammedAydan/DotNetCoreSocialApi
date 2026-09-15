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

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                throw new ArgumentException("Reason for ban must be provided.");
            }

            if (request.DurationDays.HasValue && request.DurationDays.Value <= 0)
            {
                throw new ArgumentException("Ban duration must be greater than zero days.");
            }

            var user = await _adminRepo.GetUserByIdAsync(request.TargetUserId, cancellationToken);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID '{request.TargetUserId}' was not found.");
            }

            var targetRoles = await _adminRepo.GetUserRolesAsync(user, cancellationToken);
            if (targetRoles.Any(r => string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Administrators cannot ban another administrator account.");
            }

            var success = await _adminRepo.LockUserAsync(request.TargetUserId, request.DurationDays, request.Reason, cancellationToken);
            if (!success)
            {
                throw new InvalidOperationException("Failed to ban user.");
            }

            // Align cache TTL with the DB lockout (indefinite = ~100 years).
            var blacklistTtl = TimeSpan.FromDays(request.DurationDays ?? 36500);
            // Immediately blacklist user in distributed cache for instantaneous token revocation
            await _cache.SetAsync($"blacklisted_user:{request.TargetUserId}", true, blacklistTtl);

            var durationSuffix = request.DurationDays.HasValue
                ? $"(Duration: {request.DurationDays.Value} days)"
                : "(Duration: indefinite)";
            await _auditRepo.AddAsync(new AuditLog
            {
                AdminId = request.AdminId,
                AdminEmail = request.AdminEmail,
                ActionType = "UserBanned",
                TargetEntity = "User",
                TargetId = request.TargetUserId,
                Reason = $"{request.Reason} {durationSuffix}",
                TimestampUtc = DateTime.UtcNow
            }, cancellationToken);

            return true;
        }
    }
}
