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
        Task DeleteAsync(string id, CancellationToken cancellationToken = default);
        
        Task<Notification?> GetNotificationByQueryAsync(Expression<Func<Notification, bool>> query, CancellationToken cancellationToken = default);

        Task DeleteAllForUserAsync(string userId, CancellationToken cancellationToken = default);
    }
}
