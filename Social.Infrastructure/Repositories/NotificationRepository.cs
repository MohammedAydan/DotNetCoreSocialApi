using Microsoft.EntityFrameworkCore;
using Social.Core;
using Social.Core.Entities;
using Social.Core.Interfaces;
using Social.Infrastructure.Data;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Social.Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly ApplicationDbContext _context;

        public NotificationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            // 1. Central block gate: never deliver notifications across a block.
            if (!string.IsNullOrWhiteSpace(notification.UserId) &&
                !string.IsNullOrWhiteSpace(notification.RecipientId))
            {
                var isBlocked = await _context.BlockUsers.AsNoTracking().AnyAsync(b =>
                    (b.UserId == notification.UserId && b.BlockedUserId == notification.RecipientId) ||
                    (b.UserId == notification.RecipientId && b.BlockedUserId == notification.UserId), cancellationToken);
                if (isBlocked)
                    return;
            }

            var utcNow = DateTime.UtcNow;
            var isModeration = string.Equals(notification.Type, "ModerationNotice", StringComparison.Ordinal);

            // 2. Self-actions never notify (moderation is sent by the system on behalf of admins).
            if (!isModeration &&
                !string.IsNullOrWhiteSpace(notification.UserId) &&
                string.Equals(notification.UserId, notification.RecipientId, StringComparison.OrdinalIgnoreCase))
                return;

            // 3. Per-user type toggles. Moderation bypasses toggles (safety-critical).
            var preference = string.IsNullOrWhiteSpace(notification.RecipientId)
                ? null
                : await _context.NotificationPreferences.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.UserId == notification.RecipientId, cancellationToken);
            if (!isModeration && preference != null && !preference.IsTypeEnabled(notification.Type ?? string.Empty))
                return;

            notification.Priority = NotificationPriority.For(notification.Type);
            notification.CreatedAt = notification.CreatedAt == default ? utcNow : notification.CreatedAt;

            // Resolve a display name for aggregated messages ("X and N others ...").
            if (string.IsNullOrWhiteSpace(notification.LastActorName) && !string.IsNullOrWhiteSpace(notification.UserId))
            {
                notification.LastActorName = await _context.Users.AsNoTracking()
                    .Where(u => u.Id == notification.UserId)
                    .Select(u => u.UserName)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            // 4. Quiet hours: persist immediately but hide from inbox/badge until released.
            // Moderation notices are never deferred.
            if (!isModeration && preference != null && preference.IsQuietNow(utcNow))
                notification.IsDeferred = true;

            // 5. Aggregation: collapse into the surviving unread row of the same group.
            var groupKey = (preference?.DigestEnabled == true && NotificationGrouping.IsDigestible(notification.Type))
                ? NotificationGrouping.BuildDigestKey(notification.Type, utcNow)
                : NotificationGrouping.BuildGroupKey(notification.Type, notification.PostId, notification.UserId, notification.UserId);
            if (groupKey != null)
            {
                notification.GroupKey = groupKey;
                var existing = await _context.Notifications
                    .Where(n => n.RecipientId == notification.RecipientId
                        && !n.IsRead && !n.IsDeleted
                        && n.GroupKey == groupKey)
                    .OrderByDescending(n => n.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);
                if (existing != null)
                {
                    existing.ActorCount += 1;
                    if (!string.IsNullOrWhiteSpace(notification.LastActorName))
                        existing.LastActorName = notification.LastActorName;
                    existing.Message = NotificationGrouping.BuildAggregatedMessage(
                        existing.Type,
                        existing.LastActorName ?? notification.LastActorName ?? string.Empty,
                        existing.ActorCount,
                        existing.Message);
                    existing.UpdatedAt = utcNow;
                    existing.PostId ??= notification.PostId;
                    existing.CommentId ??= notification.CommentId;
                    existing.LikeId = notification.LikeId ?? existing.LikeId;
                    existing.FollowId = notification.FollowId ?? existing.FollowId;
                    existing.FollowerId = notification.FollowerId ?? existing.FollowerId;
                    existing.ImageUrl = notification.ImageUrl ?? existing.ImageUrl;
                    await _context.SaveChangesAsync(cancellationToken);
                    return;
                }
            }

            await _context.Notifications.AddAsync(notification, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAllForUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            var notifications = await _context.Notifications
                .Where(n => n.RecipientId == userId)
                .ToListAsync(cancellationToken);

            _context.Notifications.RemoveRange(notifications);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var notification = await _context.Notifications.FindAsync([id], cancellationToken);
            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<IEnumerable<Notification>> GetAllAsync(int page = 1, int limit = 20, CancellationToken cancellationToken = default)
        {
            return await _context.Notifications
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        public async Task<Notification?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            return await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<Notification>> GetByUserIdAsync(string userId, int page = 1, int limit = 20, CancellationToken cancellationToken = default)
        {
            return await _context.Notifications
                .Where(n => n.RecipientId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Include(n => n.SenderUser)
                .ToListAsync(cancellationToken);
        }

        public async Task<(IEnumerable<Notification> Notifications, int TotalCount)> GetPagedByUserIdAsync(string userId, int page = 1, int limit = 20, CancellationToken cancellationToken = default)
        {
            var query = _context.Notifications
                .Where(n => n.RecipientId == userId)
                .OrderByDescending(n => n.CreatedAt);

            var total = await query.CountAsync(cancellationToken);
            var notifications = await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync(cancellationToken);

            return (notifications, total);
        }

        public async Task<IEnumerable<Notification>> GetUnreadByUserIdAsync(string userId, int page = 1, int limit = 20, CancellationToken cancellationToken = default)
        {
            return await _context.Notifications
                .Where(n => n.RecipientId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        public async Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default)
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.RecipientId == userId && !n.IsRead)
                .ToListAsync(cancellationToken);

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.IsDeferred = false;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task MarkAsReadAsync(string id, CancellationToken cancellationToken = default)
        {
            var notification = await _context.Notifications.FindAsync([id], cancellationToken);
            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<Notification?> GetNotificationByQueryAsync(Expression<Func<Notification, bool>> query, CancellationToken cancellationToken = default)
        {
            return await _context.Notifications.FirstOrDefaultAsync(query, cancellationToken);
        }

        public async Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _context.Notifications
                .Where(n => n.RecipientId == userId && !n.IsRead && !n.IsDeleted && !n.IsDeferred)
                .CountAsync(cancellationToken);
        }

        public async Task<(IEnumerable<Notification> Notifications, int TotalCount)> GetInboxAsync(
            string userId, string? type = null, bool unreadOnly = false,
            int page = 1, int limit = 20, CancellationToken cancellationToken = default)
        {
            page = Math.Max(1, page);
            limit = Math.Clamp(limit, 1, 100);

            var query = _context.Notifications
                .Where(n => n.RecipientId == userId && !n.IsDeleted && !n.IsDeferred);

            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(n => n.Type == type);
            if (unreadOnly)
                query = query.Where(n => !n.IsRead);

            var ordered = query
                .OrderBy(n => n.IsRead)
                .ThenByDescending(n => n.Priority)
                .ThenByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.Id);

            var total = await ordered.CountAsync(cancellationToken);
            var items = await ordered
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync(cancellationToken);

            return (items, total);
        }

        public async Task<NotificationPreference?> GetPreferenceAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _context.NotificationPreferences
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        }

        public async Task<NotificationPreference> UpsertPreferenceAsync(NotificationPreference preference, CancellationToken cancellationToken = default)
        {
            if (preference == null) throw new ArgumentNullException(nameof(preference));
            var existing = await _context.NotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == preference.UserId, cancellationToken);
            if (existing == null)
            {
                preference.UpdatedAt = DateTime.UtcNow;
                await _context.NotificationPreferences.AddAsync(preference, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                return preference;
            }

            existing.LikeEnabled = preference.LikeEnabled;
            existing.CommentEnabled = preference.CommentEnabled;
            existing.ReplyEnabled = preference.ReplyEnabled;
            existing.FollowEnabled = preference.FollowEnabled;
            existing.FollowRequestEnabled = preference.FollowRequestEnabled;
            existing.ShareEnabled = preference.ShareEnabled;
            existing.MentionEnabled = preference.MentionEnabled;
            existing.QuietStartHourUtc = preference.QuietStartHourUtc;
            existing.QuietEndHourUtc = preference.QuietEndHourUtc;
            existing.DigestEnabled = preference.DigestEnabled;
            existing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return existing;
        }

        public async Task<int> ReleaseDeferredAsync(string userId, CancellationToken cancellationToken = default)
        {
            var deferred = await _context.Notifications
                .Where(n => n.RecipientId == userId && n.IsDeferred && !n.IsDeleted)
                .ToListAsync(cancellationToken);
            foreach (var notification in deferred)
                notification.IsDeferred = false;
            await _context.SaveChangesAsync(cancellationToken);
            return deferred.Count;
        }
    }
}
