using MediatR;
using Social.Core.Entities;
using Social.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Social.Application.Features.Admin.Users.Commands
{
    public record ToggleUserVerificationCommand(
        string AdminId,
        string? AdminEmail,
        string TargetUserId,
        bool IsVerified,
        string Reason
    ) : IRequest<bool>;

    public class ToggleUserVerificationCommandHandler : IRequestHandler<ToggleUserVerificationCommand, bool>
    {
        private readonly IAdminRepository _adminRepo;
        private readonly IAuditLogRepository _auditRepo;

        public ToggleUserVerificationCommandHandler(IAdminRepository adminRepo, IAuditLogRepository auditRepo)
        {
            _adminRepo = adminRepo;
            _auditRepo = auditRepo;
        }

        public async Task<bool> Handle(ToggleUserVerificationCommand request, CancellationToken cancellationToken)
        {
            var user = await _adminRepo.GetUserByIdAsync(request.TargetUserId, cancellationToken);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID '{request.TargetUserId}' was not found.");
            }

            var success = await _adminRepo.ToggleUserVerificationAsync(request.TargetUserId, request.IsVerified, cancellationToken);
            if (!success)
            {
                throw new InvalidOperationException("Failed to update user verification status.");
            }

            await _auditRepo.AddAsync(new AuditLog
            {
                AdminId = request.AdminId,
                AdminEmail = request.AdminEmail,
                ActionType = "UserVerified",
                TargetEntity = "User",
                TargetId = request.TargetUserId,
                Reason = request.Reason,
                TimestampUtc = DateTime.UtcNow
            }, cancellationToken);

            return true;
        }
    }
}
