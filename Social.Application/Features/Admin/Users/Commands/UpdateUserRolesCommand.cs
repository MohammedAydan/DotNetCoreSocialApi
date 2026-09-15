using MediatR;
using Social.Core.Entities;
using Social.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Social.Application.Features.Admin.Users.Commands
{
    public record UpdateUserRolesCommand(
        string AdminId,
        string? AdminEmail,
        string TargetUserId,
        List<string> Roles,
        string Reason
    ) : IRequest<bool>;

    public class UpdateUserRolesCommandHandler : IRequestHandler<UpdateUserRolesCommand, bool>
    {
        private static readonly HashSet<string> ValidRoles = new(StringComparer.OrdinalIgnoreCase) { "Admin", "Moderator", "User" };
        private readonly IAdminRepository _adminRepo;
        private readonly IAuditLogRepository _auditRepo;

        public UpdateUserRolesCommandHandler(IAdminRepository adminRepo, IAuditLogRepository auditRepo)
        {
            _adminRepo = adminRepo;
            _auditRepo = auditRepo;
        }

        public async Task<bool> Handle(UpdateUserRolesCommand request, CancellationToken cancellationToken)
        {
            if (request.Roles == null || request.Roles.Count == 0 || request.Roles.Any(r => !ValidRoles.Contains(r)))
            {
                throw new ArgumentException("Invalid role or empty roles list specified.");
            }

            if (string.Equals(request.AdminId, request.TargetUserId, StringComparison.OrdinalIgnoreCase))
            {
                if (!request.Roles.Any(r => string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase)))
                {
                    throw new ArgumentException("Administrators cannot remove the Admin role from their own account.");
                }
            }

            var user = await _adminRepo.GetUserByIdAsync(request.TargetUserId, cancellationToken);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID '{request.TargetUserId}' was not found.");
            }

            var normalizedRoles = request.Roles.Select(r =>
            {
                if (string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase)) return "Admin";
                if (string.Equals(r, "Moderator", StringComparison.OrdinalIgnoreCase)) return "Moderator";
                return "User";
            }).Distinct().ToList();

            var success = await _adminRepo.UpdateUserRolesAsync(request.TargetUserId, normalizedRoles, cancellationToken);
            if (!success)
            {
                throw new InvalidOperationException("Failed to update user roles.");
            }

            await _auditRepo.AddAsync(new AuditLog
            {
                AdminId = request.AdminId,
                AdminEmail = request.AdminEmail,
                ActionType = "UserRolesUpdated",
                TargetEntity = "User",
                TargetId = request.TargetUserId,
                Reason = request.Reason,
                TimestampUtc = DateTime.UtcNow
            }, cancellationToken);

            return true;
        }
    }
}
