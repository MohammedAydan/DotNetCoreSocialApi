using Social.Core.Entities;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Social.Core.Interfaces
{
    public interface INotificationRepository
    {
        Task<Notification?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Notification>> GetAllAsync(int page = 1, int limit = 20, CancellationToken cancellationToken = default);
        Task<IEnumerable<Notification>> GetByUserIdAsync(string userId, int page = 1, int limit = 20, CancellationToken cancellationToken = default);
        Task<IEnumerable<Notification>> GetUnreadByUserIdAsync(string userId, int page = 1, int limit = 20, CancellationToken cancellationToken = default);

        Task<(IEnumerable<Notification> Notifications, int TotalCount)> GetPagedByUserIdAsync(string userId, int page = 1, int limit = 20, CancellationToken cancellationToken = default);

        Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
        Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default);
        Task MarkAsReadAsync(string id, CancellationToken cancellationToken = default);
        Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default);
        Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);

        // Smart inbox: unread first, then priority, then newest. Deferred (quiet-hours) rows excluded.
        Task<(IEnumerable<Notification> Notifications, int TotalCount)> GetInboxAsync(
            string userId, string? type = null, bool unreadOnly = false,
            int page = 1, int limit = 20, CancellationToken cancellationToken = default);

        Task<NotificationPreference?> GetPreferenceAsync(string userId, CancellationToken cancellationToken = default);
        Task<NotificationPreference> UpsertPreferenceAsync(NotificationPreference preference, CancellationToken cancellationToken = default);

        // Un-defer quiet-hours rows once the window has passed. Returns released count.
        Task<int> ReleaseDeferredAsync(string userId, CancellationToken cancellationToken = default);
        Task DeleteAsync(string id, CancellationToken cancellationToken = default);
        
        Task<Notification?> GetNotificationByQueryAsync(Expression<Func<Notification, bool>> query, CancellationToken cancellationToken = default);

        Task DeleteAllForUserAsync(string userId, CancellationToken cancellationToken = default);
    }
}
