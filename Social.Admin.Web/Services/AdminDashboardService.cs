using MediatR;
using Social.Admin.Web.Models;
using Social.Application.Features.Admin.Analytics.Queries;
using Social.Application.Features.Admin.AuditLogs.Queries;
using Social.Application.Features.Admin.Common;
using Social.Application.Features.Admin.Moderation.Commands;
using Social.Application.Features.Admin.Moderation.DTOs;
using Social.Application.Features.Admin.Moderation.Queries;
using Social.Application.Features.Admin.Users.Commands;
using Social.Application.Features.Admin.Users.DTOs;
using Social.Application.Features.Admin.Users.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Social.Admin.Web.Services
{
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly ISender _sender;
        private static readonly DateTime StartTime = DateTime.UtcNow;

        public AdminDashboardService(ISender sender)
        {
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        }

        public async Task<AdminDashboardStatsModel> GetOverviewStatsAsync(CancellationToken cancellationToken = default)
        {
            var overview = await _sender.Send(new GetPlatformOverviewQuery(), cancellationToken);
            var diagnostics = await _sender.Send(new GetSystemDiagnosticsQuery("Production"), cancellationToken);

            return new AdminDashboardStatsModel
            {
                TotalUsers = overview.TotalUsers,
                ActiveUsers24h = overview.ActiveUsers24h,
                TotalPosts = overview.TotalPosts,
                TotalComments = overview.TotalComments,
                TotalLikes = overview.TotalLikes,
                MemoryWorkingSetMb = diagnostics.MemoryWorkingSetMb,
                IsRedisConnected = diagnostics.IsConnected,
                IsMemoryFallbackActive = diagnostics.UsingMemoryFallback,
                Environment = diagnostics.Environment,
                LastUpdatedUtc = DateTime.UtcNow
            };
        }

        public async Task<PaginatedResultDto<AdminUserListItemModel>> GetUsersAsync(UserFilterModel filter, CancellationToken cancellationToken = default)
        {
            var query = new GetAdminUsersQuery(
                Page: filter.Page,
                PageSize: filter.PageSize,
                Q: filter.Query
            );

            var result = await _sender.Send(query, cancellationToken);

            var mappedItems = result.Items.Select(u => new AdminUserListItemModel
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                FirstName = u.FirstName ?? string.Empty,
                LastName = u.LastName ?? string.Empty,
                ProfileImageUrl = null,
                IsVerified = u.IsVerified,
                IsLockedOut = u.IsLockedOut,
                LockoutEnd = u.LockoutEnd,
                Roles = u.Roles,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            return new PaginatedResultDto<AdminUserListItemModel>
            {
                Items = mappedItems,
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        public async Task<bool> BanUserAsync(string adminId, string? adminEmail, BanUserModalModel model, CancellationToken cancellationToken = default)
        {
            var command = new BanUserCommand(
                AdminId: adminId,
                AdminEmail: adminEmail,
                TargetUserId: model.UserId,
                DurationDays: model.IsPermanent ? 36500 : model.DurationDays,
                Reason: model.Reason
            );

            return await _sender.Send(command, cancellationToken);
        }

        public async Task<bool> UnbanUserAsync(string adminId, string? adminEmail, string userId, string? reason, CancellationToken cancellationToken = default)
        {
            var command = new UnbanUserCommand(
                AdminId: adminId,
                AdminEmail: adminEmail,
                TargetUserId: userId,
                Reason: reason ?? "Admin unban action"
            );

            return await _sender.Send(command, cancellationToken);
        }

        public async Task<bool> UpdateUserRolesAsync(string adminId, string? adminEmail, UpdateRolesModalModel model, CancellationToken cancellationToken = default)
        {
            var roles = new List<string>();
            if (model.IsAdmin) roles.Add("Admin");
            if (model.IsModerator) roles.Add("Moderator");
            if (model.IsUser) roles.Add("User");

            var command = new UpdateUserRolesCommand(
                AdminId: adminId,
                AdminEmail: adminEmail,
                TargetUserId: model.UserId,
                Roles: roles,
                Reason: "Role assignment updated from admin dashboard"
            );

            return await _sender.Send(command, cancellationToken);
        }

        public async Task<bool> ToggleUserVerificationAsync(string adminId, string? adminEmail, string userId, bool isVerified, CancellationToken cancellationToken = default)
        {
            var command = new ToggleUserVerificationCommand(
                AdminId: adminId,
                AdminEmail: adminEmail,
                TargetUserId: userId,
                IsVerified: isVerified,
                Reason: isVerified ? "User verified by admin" : "Verification revoked by admin"
            );

            return await _sender.Send(command, cancellationToken);
        }

        public async Task<bool> ResetUserPasswordAsync(string adminId, string? adminEmail, ResetPasswordModalModel model, CancellationToken cancellationToken = default)
        {
            var command = new AdminResetPasswordCommand(
                AdminId: adminId,
                AdminEmail: adminEmail,
                TargetUserId: model.UserId,
                NewPassword: model.NewPassword,
                Reason: "Administrative password reset"
            );

            return await _sender.Send(command, cancellationToken);
        }

        public async Task<PaginatedResultDto<ModerationItemModel>> GetModerationFeedAsync(ModerationFilterModel filter, CancellationToken cancellationToken = default)
        {
            var query = new GetModerationFeedQuery(
                Page: filter.Page,
                PageSize: filter.PageSize
            );

            var result = await _sender.Send(query, cancellationToken);

            var mappedItems = result.Items.Select(m => new ModerationItemModel
            {
                Id = m.Id,
                ItemType = m.Type,
                Content = m.Content,
                AuthorId = m.AuthorId,
                AuthorUserName = m.AuthorUserName,
                AuthorProfileImageUrl = null,
                CreatedAt = m.CreatedAt,
                IsDeleted = m.IsDeleted,
                LikesCount = m.LikesCount,
                CommentsCount = m.CommentsCount,
                Media = new List<ModerationMediaModel>()
            }).ToList();

            return new PaginatedResultDto<ModerationItemModel>
            {
                Items = mappedItems,
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        public async Task<bool> ModeratePostAsync(string adminId, string? adminEmail, string postId, bool hide, string reason, CancellationToken cancellationToken = default)
        {
            if (hide)
            {
                return await _sender.Send(new HidePostCommand(adminId, adminEmail, postId, reason), cancellationToken);
            }
            else
            {
                return await _sender.Send(new RestorePostCommand(adminId, adminEmail, postId, reason), cancellationToken);
            }
        }

        public async Task<bool> ModerateCommentAsync(string adminId, string? adminEmail, string commentId, bool hide, string reason, CancellationToken cancellationToken = default)
        {
            if (hide)
            {
                return await _sender.Send(new HideCommentCommand(adminId, adminEmail, commentId, reason), cancellationToken);
            }
            else
            {
                return await _sender.Send(new RestoreCommentCommand(adminId, adminEmail, commentId, reason), cancellationToken);
            }
        }

        public async Task<PaginatedResultDto<AuditLogItemModel>> GetAuditLogsAsync(AuditLogFilterModel filter, CancellationToken cancellationToken = default)
        {
            var query = new GetAuditLogsQuery(
                Page: filter.Page,
                PageSize: filter.PageSize,
                ActionType: filter.ActionType,
                AdminId: filter.AdminId,
                FromDate: filter.FromDate,
                ToDate: filter.ToDate
            );

            var result = await _sender.Send(query, cancellationToken);

            var mappedItems = result.Items.Select(l => new AuditLogItemModel
            {
                Id = l.Id,
                AdminId = l.AdminId,
                AdminEmail = l.AdminEmail,
                ActionType = l.ActionType,
                TargetEntity = l.TargetEntity,
                TargetId = l.TargetId,
                Reason = l.Reason ?? string.Empty,
                TimestampUtc = l.TimestampUtc
            }).ToList();

            return new PaginatedResultDto<AuditLogItemModel>
            {
                Items = mappedItems,
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        public async Task<SystemDiagnosticsModel> GetDiagnosticsAsync(CancellationToken cancellationToken = default)
        {
            var result = await _sender.Send(new GetSystemDiagnosticsQuery("Production"), cancellationToken);

            return new SystemDiagnosticsModel
            {
                IsConnected = result.IsConnected,
                UsingMemoryFallback = result.UsingMemoryFallback,
                TrackedIpCount = result.TrackedIpCount,
                ThrottledRequestsCount = result.ThrottledRequestsCount,
                MemoryWorkingSetMb = result.MemoryWorkingSetMb,
                Environment = result.Environment,
                FrameworkVersion = ".NET 9.0",
                SystemUptime = DateTime.UtcNow - StartTime,
                TimestampUtc = DateTime.UtcNow
            };
        }
    }
}
