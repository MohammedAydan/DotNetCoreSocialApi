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
    public record AdminResetPasswordCommand(
        string AdminId,
        string? AdminEmail,
        string TargetUserId,
        string? NewPassword,
        string Reason
    ) : IRequest<bool>;

    public class AdminResetPasswordCommandHandler : IRequestHandler<AdminResetPasswordCommand, bool>
    {
        private readonly IAdminRepository _adminRepo;
        private readonly IAuditLogRepository _auditRepo;

        public AdminResetPasswordCommandHandler(IAdminRepository adminRepo, IAuditLogRepository auditRepo)
        {
            _adminRepo = adminRepo;
            _auditRepo = auditRepo;
        }

        public async Task<bool> Handle(AdminResetPasswordCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.NewPassword) ||
                request.NewPassword.Length < 6 ||
                !request.NewPassword.Any(char.IsUpper) ||
                !request.NewPassword.Any(char.IsDigit))
            {
                throw new ArgumentException("Password does not meet complexity requirements (min 6 characters, uppercase, and digit required).");
            }

            var user = await _adminRepo.GetUserByIdAsync(request.TargetUserId, cancellationToken);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID '{request.TargetUserId}' was not found.");
            }

            var success = await _adminRepo.ResetUserPasswordAsync(request.TargetUserId, request.NewPassword, cancellationToken);
            if (!success)
            {
                throw new InvalidOperationException("Failed to reset user password.");
            }

            await _auditRepo.AddAsync(new AuditLog
            {
                AdminId = request.AdminId,
                AdminEmail = request.AdminEmail,
                ActionType = "PasswordReset",
                TargetEntity = "User",
                TargetId = request.TargetUserId,
                Reason = request.Reason,
                TimestampUtc = DateTime.UtcNow
            }, cancellationToken);

            return true;
        }
    }
}
