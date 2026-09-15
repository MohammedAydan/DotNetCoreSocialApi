using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Social.Application.Features.Admin.Common;
using Social.Application.Features.Admin.Users.Commands;
using Social.Application.Features.Admin.Users.DTOs;
using Social.Application.Features.Admin.Users.Queries;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Social.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : BaseController
    {
        private readonly ISender _sender;

        public AdminUsersController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? q = null)
        {
            var result = await _sender.Send(new GetAdminUsersQuery(page, pageSize, q));
            return ApiSuccess("Users retrieved successfully", result);
        }

        [HttpPost("{userId}/ban")]
        public async Task<IActionResult> BanUser(string userId, [FromBody] AdminBanUserRequest request)
        {
            var adminId = GetUserId();
            var adminEmail = GetUserEmail();
            var result = await _sender.Send(new BanUserCommand(adminId, adminEmail, userId, request?.Reason ?? string.Empty, request?.DurationDays));
            return ApiSuccess("User banned successfully", result);
        }

        [HttpPost("{userId}/unban")]
        public async Task<IActionResult> UnbanUser(string userId, [FromBody] AdminUnbanUserRequest request)
        {
            var adminId = GetUserId();
            var adminEmail = GetUserEmail();
            var result = await _sender.Send(new UnbanUserCommand(adminId, adminEmail, userId, request?.Reason ?? string.Empty));
            return ApiSuccess("User unbanned successfully", result);
        }

        [HttpPost("{userId}/roles")]
        public async Task<IActionResult> UpdateRoles(string userId, [FromBody] AdminUpdateRolesRequest request)
        {
            var adminId = GetUserId();
            var adminEmail = GetUserEmail();
            var roles = request?.Roles ?? new List<string>();
            await _sender.Send(new UpdateUserRolesCommand(adminId, adminEmail, userId, roles, request?.Reason ?? string.Empty));
            return ApiSuccess("User roles updated successfully", roles);
        }

        [HttpPost("{userId}/verify")]
        public async Task<IActionResult> ToggleVerification(string userId, [FromBody] AdminToggleVerificationRequest request)
        {
            var adminId = GetUserId();
            var adminEmail = GetUserEmail();
            var isVerified = request?.IsVerified ?? false;
            await _sender.Send(new ToggleUserVerificationCommand(adminId, adminEmail, userId, isVerified, request?.Reason ?? string.Empty));
            return ApiSuccess("User verification status updated successfully", isVerified);
        }

        [HttpPost("{userId}/reset-password")]
        public async Task<IActionResult> ResetPassword(string userId, [FromBody] AdminResetPasswordRequest request)
        {
            var adminId = GetUserId();
            var adminEmail = GetUserEmail();
            var result = await _sender.Send(new AdminResetPasswordCommand(adminId, adminEmail, userId, request?.NewPassword, request?.Reason ?? string.Empty));
            return ApiSuccess("User password reset successfully", result);
        }
    }
}
