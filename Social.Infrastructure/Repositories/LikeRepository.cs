using Microsoft.EntityFrameworkCore;
using Social.Core;
using Social.Core.Entities;
using Social.Core.Interfaces;
using Social.Infrastructure.Data;

namespace Social.Infrastructure.Repositories
{
    public class LikeRepository : ILikeRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationRepository _notificationRepository;

        public LikeRepository(ApplicationDbContext context, INotificationRepository notificationRepository)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
        }

        public async Task<bool> AddOrRemoveLikeAsync(string postId, string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(postId)) throw new ArgumentException("Post ID cannot be null or empty.", nameof(postId));
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            var existingLike = await _context.Likes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId, cancellationToken);

            var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId, cancellationToken);
            if (post == null) return false;

            if (existingLike == null)
            {
                var isBlocked = await _context.BlockUsers.AsNoTracking().AnyAsync(b =>
                    (b.UserId == userId && b.BlockedUserId == post.UserId) ||
                    (b.UserId == post.UserId && b.BlockedUserId == userId), cancellationToken);
                if (isBlocked)
                    throw new InvalidOperationException("Action not allowed between blocked users.");
            }
            
            if (existingLike != null)
            {
                await RemoveLikeAsync(existingLike, post, userId, cancellationToken);
                return true;
            }

            await AddLikeAsync(postId, userId, post, cancellationToken);
            return false;
        }

        private async Task RemoveLikeAsync(Like existingLike, Post post, string userId, CancellationToken cancellationToken)
        {
            _context.Likes.Remove(existingLike);
            post.LikesCount = Math.Max(post.LikesCount - 1, 0);

            if(post.UserId != userId)
            {
                var notification = await _notificationRepository.GetNotificationByQueryAsync(n => n.LikeId == existingLike.Id);
                if (notification != null)
                {
                    await _notificationRepository.DeleteAsync(notification.Id);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task AddLikeAsync(string postId, string userId, Post post, CancellationToken cancellationToken)
        {
            var newLike = new Like
            {
                Id = Guid.NewGuid().ToString(),
                PostId = postId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Likes.Add(newLike);
            post.LikesCount++;

            if(post.UserId != userId)
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid().ToString(),
                    RecipientId = post.UserId,
                    UserId = userId,
                    Type = NotificationActionTypes.Like,
                    Message = $"{userId} liked your post.",
                    PostId = postId,
                    LikeId = newLike.Id,
                    CreatedAt = DateTime.UtcNow
                };

                await _notificationRepository.AddAsync(notification);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IEnumerable<Like>> GetLikesByPostIdAsync(string postId, int page = 1, int limit = 20, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(postId)) throw new ArgumentException("Post ID cannot be null or empty.", nameof(postId));
            if (page <= 0) throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than zero.");
            if (limit <= 0) throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be greater than zero.");

            return await _context.Likes
                .AsNoTracking()
                .Where(l => l.PostId == postId)
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Include(l => l.User)
                .ToListAsync(cancellationToken);
        }
    }
}
