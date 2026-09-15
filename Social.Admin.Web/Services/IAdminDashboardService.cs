using Social.Admin.Web.Models;
using Social.Application.Features.Admin.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Social.Admin.Web.Services
{
    public interface IAdminDashboardService
    {
        Task<AdminDashboardStatsModel> GetOverviewStatsAsync(CancellationToken cancellationToken = default);
        Task<PaginatedResultDto<AdminUserListItemModel>> GetUsersAsync(UserFilterModel filter, CancellationToken cancellationToken = default);
        Task<bool> BanUserAsync(string adminId, string? adminEmail, BanUserModalModel model, CancellationToken cancellationToken = default);
        Task<bool> UnbanUserAsync(string adminId, string? adminEmail, string userId, string? reason, CancellationToken cancellationToken = default);
        Task<bool> UpdateUserRolesAsync(string adminId, string? adminEmail, UpdateRolesModalModel model, CancellationToken cancellationToken = default);
        Task<bool> ToggleUserVerificationAsync(string adminId, string? adminEmail, string userId, bool isVerified, CancellationToken cancellationToken = default);
        Task<bool> ResetUserPasswordAsync(string adminId, string? adminEmail, ResetPasswordModalModel model, CancellationToken cancellationToken = default);
        Task<PaginatedResultDto<ModerationItemModel>> GetModerationFeedAsync(ModerationFilterModel filter, CancellationToken cancellationToken = default);
        Task<bool> ModeratePostAsync(string adminId, string? adminEmail, string postId, bool hide, string reason, CancellationToken cancellationToken = default);
        Task<bool> ModerateCommentAsync(string adminId, string? adminEmail, string commentId, bool hide, string reason, CancellationToken cancellationToken = default);
        Task<PaginatedResultDto<AuditLogItemModel>> GetAuditLogsAsync(AuditLogFilterModel filter, CancellationToken cancellationToken = default);
        Task<SystemDiagnosticsModel> GetDiagnosticsAsync(CancellationToken cancellationToken = default);
    }
}
