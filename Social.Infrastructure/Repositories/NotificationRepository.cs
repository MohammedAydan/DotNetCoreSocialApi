using Microsoft.EntityFrameworkCore;
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
    }
}
