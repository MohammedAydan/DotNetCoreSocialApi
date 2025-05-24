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
        Task<Post> AddPostAsync(Post post);
        Task<Post> SharePostAsync(string postId, Post post);

        Task<IEnumerable<Post>> GetMyPostsAsync(string userId, int page = 1, int limit = 20);
        //Task<IEnumerable<Post>> GetPostsAsync(string userId, int page = 1, int limit = 20);
        Task<IEnumerable<Post>> GetPostsByUserIdAsync(string userId, int page = 1, int limit = 20, string? myUserId = null);

        Task<IEnumerable<Post>> GetFeedPostsAsync(string userId, int page = 1, int limit = 20);
        Task<Post> GetPostByIdAsync(string postId, string? userId = null);
        Task<Post> UpdatePostAsync(Post post);
        Task<bool> DeletePostAsync(string postId, string userId);
    }
}
