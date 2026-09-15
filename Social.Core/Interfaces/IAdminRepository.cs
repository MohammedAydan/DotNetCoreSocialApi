using Social.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Social.Core.Interfaces
{
    public record ModerationMediaRecord(
        string Id,
        string Url,
        string Type,
        string? ThumbnailUrl
    );

    public record ModerationFeedItemRecord(
        string Id,
        string Type,
        string Content,
        string AuthorId,
        string AuthorUserName,
        bool IsDeleted,
        int LikesCount,
        int CommentsCount,
        DateTime CreatedAt,
        string? Title = null,
        string? Visibility = null,
        List<ModerationMediaRecord>? Media = null,
        string? AuthorEmail = null,
        int SharesCount = 0
    );

    public interface IAdminRepository
    {
        Task<(List<User> Users, int TotalCount)> GetUsersPagedAsync(int page, int pageSize, string? q = null, CancellationToken cancellationToken = default);
        Task<User?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);
        Task<IList<string>> GetUserRolesAsync(User user, CancellationToken cancellationToken = default);
        Task<bool> LockUserAsync(string userId, int? durationDays, string reason, CancellationToken cancellationToken = default);
        Task<bool> UnlockUserAsync(string userId, CancellationToken cancellationToken = default);
        Task<bool> UpdateUserRolesAsync(string userId, List<string> roles, CancellationToken cancellationToken = default);
        Task<bool> ToggleUserVerificationAsync(string userId, bool isVerified, CancellationToken cancellationToken = default);
        Task<bool> ResetUserPasswordAsync(string userId, string newPassword, CancellationToken cancellationToken = default);
        Task RevokeUserTokensAsync(string userId, CancellationToken cancellationToken = default);

        Task<(List<ModerationFeedItemRecord> Items, int TotalCount)> GetModerationFeedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
        Task<Post?> GetPostByIdAsync(string postId, CancellationToken cancellationToken = default);
        Task<bool> HidePostAsync(string postId, CancellationToken cancellationToken = default);
        Task<bool> RestorePostAsync(string postId, CancellationToken cancellationToken = default);
        Task<bool> UpdatePostVisibilityAsync(string postId, string visibility, CancellationToken cancellationToken = default);
        Task<bool> DeletePostPermanentlyAsync(string postId, CancellationToken cancellationToken = default);
        Task<Comment?> GetCommentByIdAsync(string commentId, CancellationToken cancellationToken = default);
        Task<bool> HideCommentAsync(string commentId, CancellationToken cancellationToken = default);
        Task<bool> RestoreCommentAsync(string commentId, CancellationToken cancellationToken = default);

        Task<(int TotalUsers, int ActiveUsers24h, int TotalPosts, int TotalComments, int TotalLikes)> GetOverviewMetricsAsync(CancellationToken cancellationToken = default);
    }
}
