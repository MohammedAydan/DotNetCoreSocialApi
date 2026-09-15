using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Social.Core.Entities;
using Social.Core.Interfaces;
using Social.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Social.Infrastructure.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminRepository(
            ApplicationDbContext context,
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        }

        public async Task<(List<User> Users, int TotalCount)> GetUsersPagedAsync(
            int page,
            int pageSize,
            string? q = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var trimmed = q.Trim();
                query = query.Where(u =>
                    (u.UserName != null && u.UserName.Contains(trimmed)) ||
                    (u.Email != null && u.Email.Contains(trimmed)) ||
                    (u.FirstName != null && u.FirstName.Contains(trimmed)) ||
                    (u.LastName != null && u.LastName.Contains(trimmed)));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (users, totalCount);
        }

        public async Task<User?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        }

        public async Task<IList<string>> GetUserRolesAsync(User user, CancellationToken cancellationToken = default)
        {
            if (user == null) return new List<string>();
            return await _userManager.GetRolesAsync(user);
        }

        public async Task<bool> LockUserAsync(string userId, int? durationDays, string reason, CancellationToken cancellationToken = default)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null) return false;

            user.LockoutEnabled = true;
            user.LockoutEnd = durationDays.HasValue
                ? DateTimeOffset.UtcNow.AddDays(durationDays.Value)
                : DateTimeOffset.UtcNow.AddYears(100);

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded) return false;

            await RevokeUserTokensAsync(userId, cancellationToken);
            return true;
        }

        public async Task<bool> UnlockUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null) return false;

            user.LockoutEnd = null;
            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<bool> UpdateUserRolesAsync(string userId, List<string> roles, CancellationToken cancellationToken = default)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null) return false;

            var currentRoles = await _userManager.GetRolesAsync(user);
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded) return false;

            var addResult = await _userManager.AddToRolesAsync(user, roles);
            return addResult.Succeeded;
        }

        public async Task<bool> ToggleUserVerificationAsync(string userId, bool isVerified, CancellationToken cancellationToken = default)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null) return false;

            user.IsVerified = isVerified;
            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<bool> ResetUserPasswordAsync(string userId, string newPassword, CancellationToken cancellationToken = default)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null) return false;

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!result.Succeeded) return false;

            await _userManager.UpdateSecurityStampAsync(user);
            await RevokeUserTokensAsync(userId, cancellationToken);
            return true;
        }

        public async Task RevokeUserTokensAsync(string userId, CancellationToken cancellationToken = default)
        {
            var tokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId)
                .ToListAsync(cancellationToken);

            if (tokens.Count != 0)
            {
                _context.RefreshTokens.RemoveRange(tokens);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<(List<ModerationFeedItemRecord> Items, int TotalCount)> GetModerationFeedAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var totalPosts = await _context.Posts.CountAsync(cancellationToken);
            var totalComments = await _context.Comments.CountAsync(cancellationToken);
            var totalCount = totalPosts + totalComments;

            var fetchLimit = Math.Max(pageSize, page * pageSize);

            var recentPosts = await _context.Posts.AsNoTracking()
                .Include(p => p.User)
                .Include(p => p.Media)
                .OrderByDescending(p => p.CreatedAt)
                .Take(fetchLimit)
                .Select(p => new ModerationFeedItemRecord(
                    p.Id,
                    "Post",
                    p.Content ?? string.Empty,
                    p.UserId,
                    p.User != null ? (p.User.UserName ?? string.Empty) : string.Empty,
                    p.IsDeleted,
                    p.LikesCount,
                    p.CommentsCount,
                    p.CreatedAt,
                    p.Title,
                    p.Visibility,
                    p.Media.Select(m => new ModerationMediaRecord(m.Id, m.Url, m.Type, m.ThumbnailUrl)).ToList(),
                    p.User != null ? p.User.Email : null,
                    p.ShareingsCount
                ))
                .ToListAsync(cancellationToken);

            var recentComments = await _context.Comments.AsNoTracking()
                .Include(c => c.User)
                .OrderByDescending(c => c.CreatedAt)
                .Take(fetchLimit)
                .Select(c => new ModerationFeedItemRecord(
                    c.Id,
                    "Comment",
                    c.Content ?? string.Empty,
                    c.UserId,
                    c.User != null ? (c.User.UserName ?? string.Empty) : string.Empty,
                    c.IsDeleted,
                    0,
                    c.RepliesCount,
                    c.CreatedAt,
                    null,
                    null,
                    null,
                    c.User != null ? c.User.Email : null,
                    0
                ))
                .ToListAsync(cancellationToken);

            var items = recentPosts.Concat(recentComments)
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (items, totalCount);
        }

        public async Task<Post?> GetPostByIdAsync(string postId, CancellationToken cancellationToken = default)
        {
            return await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Media)
                .FirstOrDefaultAsync(p => p.Id == postId, cancellationToken);
        }

        public async Task<bool> HidePostAsync(string postId, CancellationToken cancellationToken = default)
        {
            var post = await _context.Posts.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == postId, cancellationToken);
            if (post == null) return false;

            post.IsDeleted = true;
            if (post.User != null)
            {
                post.User.PostsCount = Math.Max(0, post.User.PostsCount - 1);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> RestorePostAsync(string postId, CancellationToken cancellationToken = default)
        {
            var post = await _context.Posts.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == postId, cancellationToken);
            if (post == null) return false;

            post.IsDeleted = false;
            if (post.User != null)
            {
                post.User.PostsCount++;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> UpdatePostVisibilityAsync(string postId, string visibility, CancellationToken cancellationToken = default)
        {
            var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId, cancellationToken);
            if (post == null) return false;

            post.Visibility = visibility;
            post.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> DeletePostPermanentlyAsync(string postId, CancellationToken cancellationToken = default)
        {
            var post = await _context.Posts
                .Include(p => p.Media)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == postId, cancellationToken);
            if (post == null) return false;

            if (post.User != null && !post.IsDeleted)
            {
                post.User.PostsCount = Math.Max(0, post.User.PostsCount - 1);
            }

            if (post.Media != null && post.Media.Count != 0)
            {
                _context.Media.RemoveRange(post.Media);
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<Comment?> GetCommentByIdAsync(string commentId, CancellationToken cancellationToken = default)
        {
            return await _context.Comments
                .Include(c => c.Post)
                .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);
        }

        public async Task<bool> HideCommentAsync(string commentId, CancellationToken cancellationToken = default)
        {
            var comment = await _context.Comments.Include(c => c.Post).FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);
            if (comment == null) return false;

            comment.IsDeleted = true;
            if (comment.Post != null)
            {
                comment.Post.CommentsCount = Math.Max(0, comment.Post.CommentsCount - 1);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> RestoreCommentAsync(string commentId, CancellationToken cancellationToken = default)
        {
            var comment = await _context.Comments.Include(c => c.Post).FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);
            if (comment == null) return false;

            comment.IsDeleted = false;
            if (comment.Post != null)
            {
                comment.Post.CommentsCount++;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<(int TotalUsers, int ActiveUsers24h, int TotalPosts, int TotalComments, int TotalLikes)> GetOverviewMetricsAsync(CancellationToken cancellationToken = default)
        {
            var totalUsers = await _context.Users.CountAsync(cancellationToken);
            var since24h = DateTime.UtcNow.AddHours(-24);
            var activeUsers24h = await _context.Users.CountAsync(u => u.UpdatedAt >= since24h, cancellationToken);
            var totalPosts = await _context.Posts.CountAsync(p => !p.IsDeleted, cancellationToken);
            var totalComments = await _context.Comments.CountAsync(c => !c.IsDeleted, cancellationToken);
            var totalLikes = await _context.Likes.CountAsync(cancellationToken);

            return (totalUsers, activeUsers24h, totalPosts, totalComments, totalLikes);
        }
    }
}
