using Microsoft.EntityFrameworkCore;
using Social.Core.Entities;
using Social.Core.Interfaces;
using Social.Infrastructure.Data;
using Social.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Social.Infrastructure.Repositories
{
    public class FollowRepository : IFollowRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationRepository _notificationRepository;

        public FollowRepository(ApplicationDbContext context, INotificationRepository notificationRepository)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
        }

        public async Task<Follower> FollowUserAsync(string followerId, string targetUserId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(followerId))
                throw new ArgumentException("Follower ID cannot be null or empty.", nameof(followerId));

            if (string.IsNullOrWhiteSpace(targetUserId))
                throw new ArgumentException("Target user ID cannot be null or empty.", nameof(targetUserId));

            if (followerId == targetUserId)
                throw new InvalidOperationException("You cannot follow yourself.");

            var targetUser = await _context.Users.FindAsync([targetUserId], cancellationToken);
            if (targetUser == null)
                throw new InvalidOperationException("Target user not found.");

            var isBlocked = await _context.BlockUsers.AsNoTracking().AnyAsync(b =>
                (b.UserId == followerId && b.BlockedUserId == targetUserId) ||
                (b.UserId == targetUserId && b.BlockedUserId == followerId), cancellationToken);
            if (isBlocked)
                throw new InvalidOperationException("Action not allowed between blocked users.");

            var existing = await _context.Followers
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == targetUserId, cancellationToken);
            if (existing != null)
                throw new InvalidOperationException("You already follow or have a pending request.");

            bool isAccepted = !targetUser.IsPrivate;

            var follow = new Follower
            {
                Id = Guid.NewGuid().ToString(),
                FollowerId = followerId,
                FollowingId = targetUserId,
                Accepted = isAccepted,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Followers.Add(follow);

            if (isAccepted)
            {
                // Update follower's following count (people I follow)
                var followerUser = await _context.Users.FindAsync([followerId], cancellationToken);
                if (followerUser != null)
                {
                    followerUser.FollowingCount++;
                    _context.Users.Update(followerUser);
                }

                // Update target user's followers count (people who follow me)
                targetUser.FollowersCount++;
                _context.Users.Update(targetUser);

                // Create notification for accepted follow
                await CreateFollowNotificationAsync(followerId, targetUserId, follow.Id, NotificationActionTypes.Follow);
            }
            else
            {
                // Create notification for follow request
                await CreateFollowNotificationAsync(followerId, targetUserId, follow.Id, NotificationActionTypes.FollowRequest);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return follow;
        }

        public async Task<bool> UnfollowUserAsync(string followerId, string targetUserId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(followerId))
                throw new ArgumentException("Follower ID cannot be null or empty.", nameof(followerId));

            if (string.IsNullOrWhiteSpace(targetUserId))
                throw new ArgumentException("Target user ID cannot be null or empty.", nameof(targetUserId));

            var follow = await _context.Followers
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == targetUserId, cancellationToken);

            if (follow == null)
                throw new InvalidOperationException("Follow relationship not found.");

            bool wasAccepted = follow.Accepted;

            _context.Followers.Remove(follow);

            if (wasAccepted)
            {
                // Update follower's following count (people I follow)
                var followerUser = await _context.Users.FindAsync([followerId], cancellationToken);
                if (followerUser != null && followerUser.FollowingCount > 0)
                {
                    followerUser.FollowingCount--;
                    _context.Users.Update(followerUser);
                }

                // Update target user's followers count (people who follow me)
                var targetUser = await _context.Users.FindAsync([targetUserId], cancellationToken);
                if (targetUser != null && targetUser.FollowersCount > 0)
                {
                    targetUser.FollowersCount--;
                    _context.Users.Update(targetUser);
                }
            }

            // Delete related notification if exists
            var notification = await _notificationRepository.GetNotificationByQueryAsync(n => n.FollowId == follow.Id);
            if (notification != null)
            {
                await _notificationRepository.DeleteAsync(notification.Id);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> AcceptFollowRequestAsync(string targetUserId, string followerId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(followerId))
                throw new ArgumentException("Follower ID cannot be null or empty.", nameof(followerId));

            if (string.IsNullOrWhiteSpace(targetUserId))
                throw new ArgumentException("Target user ID cannot be null or empty.", nameof(targetUserId));

            var follow = await _context.Followers
                .FirstOrDefaultAsync(f =>
                    f.FollowingId == targetUserId &&
                    f.FollowerId == followerId &&
                    !f.Accepted, cancellationToken);

            if (follow == null)
                throw new InvalidOperationException("No pending follow request found.");

            var isBlocked = await _context.BlockUsers.AsNoTracking().AnyAsync(b =>
                (b.UserId == targetUserId && b.BlockedUserId == followerId) ||
                (b.UserId == followerId && b.BlockedUserId == targetUserId), cancellationToken);
            if (isBlocked)
                throw new InvalidOperationException("Action not allowed between blocked users.");

            follow.Accepted = true;
            follow.UpdatedAt = DateTime.UtcNow;

            // Update target user's followers count (people who follow me)
            var targetUser = await _context.Users.FindAsync([targetUserId], cancellationToken);
            if (targetUser != null)
            {
                targetUser.FollowersCount++;
                _context.Users.Update(targetUser);
            }

            // Update follower's following count (people I follow)
            var followerUser = await _context.Users.FindAsync([followerId], cancellationToken);
            if (followerUser != null)
            {
                followerUser.FollowingCount++;
                _context.Users.Update(followerUser);
            }

            // Update existing follow request notification to accepted
            var existingNotification = await _notificationRepository.GetNotificationByQueryAsync(n => n.FollowId == follow.Id);
            if (existingNotification != null)
            {
                await _notificationRepository.DeleteAsync(existingNotification.Id);
            }

            // Create new notification for accepted follow request
            await CreateFollowNotificationAsync(followerId, targetUserId, follow.Id, NotificationActionTypes.AcceptFollowRequest);

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> RejectFollowRequestAsync(string targetUserId, string followerId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(followerId))
                throw new ArgumentException("Follower ID cannot be null or empty.", nameof(followerId));

            if (string.IsNullOrWhiteSpace(targetUserId))
                throw new ArgumentException("Target user ID cannot be null or empty.", nameof(targetUserId));

            var follow = await _context.Followers
                .FirstOrDefaultAsync(f =>
                    f.FollowingId == targetUserId &&
                    f.FollowerId == followerId &&
                    !f.Accepted, cancellationToken);

            if (follow == null)
                throw new InvalidOperationException("No pending follow request found.");

            // Delete related notification if exists
            var notification = await _notificationRepository.GetNotificationByQueryAsync(n => n.FollowId == follow.Id);
            if (notification != null)
            {
                await _notificationRepository.DeleteAsync(notification.Id);
            }

            _context.Followers.Remove(follow);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<IEnumerable<Follower>> GetFollowersAsync(string userId, int page = 1, int limit = 20, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            if (page <= 0)
                throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than zero.");

            if (limit <= 0)
                throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be greater than zero.");

            return await _context.Followers
                .AsNoTracking()
                .Include(f => f.FollowerUser) // People who follow me (my followers)
                .Where(f => f.FollowingId == userId && f.Accepted)
                .Where(f => !_context.BlockUsers.Any(b =>
                    (b.UserId == userId && b.BlockedUserId == f.FollowerId) ||
                    (b.UserId == f.FollowerId && b.BlockedUserId == userId)))
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Follower>> GetFollowingAsync(string userId, int page = 1, int limit = 20, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            if (page <= 0)
                throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than zero.");

            if (limit <= 0)
                throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be greater than zero.");

            return await _context.Followers
                .AsNoTracking()
                .Include(f => f.FollowingUser) // People I follow (my following)
                .Where(f => f.FollowerId == userId && f.Accepted)
                .Where(f => !_context.BlockUsers.Any(b =>
                    (b.UserId == userId && b.BlockedUserId == f.FollowingId) ||
                    (b.UserId == f.FollowingId && b.BlockedUserId == userId)))
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Follower>> GetPendingFollowRequestsAsync(string userId, int page = 1, int limit = 20, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            if (page <= 0)
                throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than zero.");

            if (limit <= 0)
                throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be greater than zero.");

            return await _context.Followers
                .AsNoTracking()
                .Include(f => f.FollowerUser) // People who want to follow me (pending requests)
                .Where(f => f.FollowingId == userId && !f.Accepted)
                .Where(f => !_context.BlockUsers.Any(b =>
                    (b.UserId == userId && b.BlockedUserId == f.FollowerId) ||
                    (b.UserId == f.FollowerId && b.BlockedUserId == userId)))
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        private async Task CreateFollowNotificationAsync(string followerId, string targetUserId, string followId, string notificationType)
        {
            // Never notify across a block in either direction.
            var isBlocked = await _context.BlockUsers.AsNoTracking().AnyAsync(b =>
                (b.UserId == followerId && b.BlockedUserId == targetUserId) ||
                (b.UserId == targetUserId && b.BlockedUserId == followerId));
            if (isBlocked)
                return;

            string message;
            string recipientId;

            switch (notificationType)
            {
                case NotificationActionTypes.Follow:
                    message = $"{followerId} started following you.";
                    recipientId = targetUserId;
                    break;
                case NotificationActionTypes.FollowRequest:
                    message = $"{followerId} sent you a follow request.";
                    recipientId = targetUserId;
                    break;
                case NotificationActionTypes.AcceptFollowRequest:
                    message = $"{targetUserId} accepted your follow request.";
                    recipientId = followerId;
                    break;
                default:
                    return;
            }

            var notification = new Notification
            {
                Id = Guid.NewGuid().ToString(),
                RecipientId = recipientId,
                UserId = followerId,
                Type = notificationType,
                Message = message,
                FollowId = followId,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(notification);
        }
    }
}