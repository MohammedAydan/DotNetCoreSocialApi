using Social.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Core.Interfaces
{
    public interface ILikeRepository
    {
        Task<bool> AddOrRemoveLikeAsync(string postId, string userId, CancellationToken cancellationToken = default);

        Task<IEnumerable<Like>> GetLikesByPostIdAsync(string postId, int page = 1, int limit = 20, CancellationToken cancellationToken = default);
    }
}
