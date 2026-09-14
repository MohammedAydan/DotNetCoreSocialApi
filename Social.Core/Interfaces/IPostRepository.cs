using Social.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Core.Interfaces
{
    public interface IPostRepository
    {
        Task<Post> AddPostAsync(Post post, CancellationToken cancellationToken = default);
        Task<Post> SharePostAsync(string postId, Post post, CancellationToken cancellationToken = default);

        Task<IEnumerable<Post>> GetMyPostsAsync(string userId, int page = 1, int limit = 20, CancellationToken cancellationToken = default);
        Task<IEnumerable<Post>> GetPostsByUserIdAsync(string userId, int page = 1, int limit = 20, string? myUserId = null, CancellationToken cancellationToken = default);

        Task<IEnumerable<Post>> GetFeedPostsAsync(string userId, int page = 1, int limit = 20, CancellationToken cancellationToken = default);
        Task<Post> GetPostByIdAsync(string postId, string? userId = null, CancellationToken cancellationToken = default);
        Task<Post> UpdatePostAsync(Post post, CancellationToken cancellationToken = default);
        Task<bool> DeletePostAsync(string postId, string userId, CancellationToken cancellationToken = default);
    }
}
