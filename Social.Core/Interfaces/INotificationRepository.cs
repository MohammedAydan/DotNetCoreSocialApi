using Social.Core.Entities;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Social.Core.Interfaces
{
    public interface INotificationRepository
    {
        Task<Notification?> GetByIdAsync(string id);
        Task<IEnumerable<Notification>> GetAllAsync(int page = 1, int limit = 20);
        Task<IEnumerable<Notification>> GetByUserIdAsync(string userId, int page = 1, int limit = 20);
        Task<IEnumerable<Notification>> GetUnreadByUserIdAsync(string userId, int page = 1, int limit = 20);

        Task<(IEnumerable<Notification> Notifications, int TotalCount)> GetPagedByUserIdAsync(string userId, int page = 1, int limit = 20);

        Task AddAsync(Notification notification);
        Task UpdateAsync(Notification notification);
        Task MarkAsReadAsync(string id);
        Task MarkAllAsReadAsync(string userId);
        Task DeleteAsync(string id);
        
        Task<Notification?> GetNotificationByQueryAsync(Expression<Func<Notification, bool>> query);

        Task DeleteAllForUserAsync(string userId);
    }
}
