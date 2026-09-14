using Social.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Core.Interfaces
{
    public interface IFollowRepository
    {
        Task<Follower> FollowUserAsync(string followerId, string targetUserId, CancellationToken cancellationToken = default);
        Task<bool> UnfollowUserAsync(string followerId, string targetUserId, CancellationToken cancellationToken = default);

        Task<bool> AcceptFollowRequestAsync(string targetUserId, string followerId, CancellationToken cancellationToken = default);
        Task<bool> RejectFollowRequestAsync(string targetUserId, string followerId, CancellationToken cancellationToken = default);

        Task<IEnumerable<Follower>> GetFollowersAsync(string userId, int page = 1, int limit = 20, CancellationToken cancellationToken = default);
        Task<IEnumerable<Follower>> GetFollowingAsync(string userId, int page = 1, int limit = 20, CancellationToken cancellationToken = default);
        Task<IEnumerable<Follower>> GetPendingFollowRequestsAsync(string userId, int page = 1, int limit = 20, CancellationToken cancellationToken = default);
    }
}
