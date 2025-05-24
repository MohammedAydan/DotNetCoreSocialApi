using Microsoft.EntityFrameworkCore;
using Social.Core.Interfaces;
using Social.Infrastucture.Data;
using Social.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Social.Core;
using System.Linq.Expressions;

namespace Social.Infrastucture.Repositories
{
    public class PostRepository(ApplicationDbContext _context) : IPostRepository
    {

        public async Task<Post> AddPostAsync(Post post)
        {
            post.Id = Guid.NewGuid().ToString();
            post.CreatedAt = DateTime.UtcNow;
            _context.Posts.Add(post);

            var user = await _context.Users.FindAsync(post.UserId);
            if (user != null)
            {
                user.PostsCount++;
                _context.Users.Update(user);
            }

            await _context.SaveChangesAsync();
            return post;
        }

        public async Task<Post> SharePostAsync(string postId, Post post) 
        {
            post.Id = Guid.NewGuid().ToString();
            post.CreatedAt = DateTime.UtcNow;
            post.ParentPostId = postId;
            _context.Posts.Add(post);

            var user = await _context.Users.FindAsync(post.UserId);
            if (user != null)
            {
                user.PostsCount++;
                _context.Users.Update(user);
            }

            var originalPost = await _context.Posts
                .Where(p => p.Id.Equals(postId))
                .Include(p => p.ParentPost)
                .ThenInclude(p => p.ParentPost)
                .ThenInclude(p => p.User)
                .FirstOrDefaultAsync();

            if (originalPost != null) {
                post.ParentPost = originalPost;
                originalPost.ShareingsCount = originalPost.ShareingsCount + 1;
                _context.Posts.Update(originalPost);
            }

            await _context.SaveChangesAsync();
            return post;
        }


        public async Task<bool> DeletePostAsync(string postId, string userId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null || post.UserId != userId)
                return false;

            _context.Posts.Remove(post);

            var user = await _context.Users.FindAsync(post.UserId);
            if (user != null && user.PostsCount > 0)
            {
                user.PostsCount--;
                _context.Users.Update(user);
            }

            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<IEnumerable<Post>> GetFeedPostsAsync(string userId, int page = 1, int limit = 20)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            if (page < 1)
                throw new ArgumentOutOfRangeException(nameof(page), "Page number must be greater than 0.");

            if (limit < 1)
                throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be greater than 0.");

            // Use a more efficient SQL query with joins instead of multiple queries
            // This executes a single optimized query that the database can better handle
            var query = from p in _context.Posts
                        join u in _context.Users on p.UserId equals u.Id
                        join f in _context.Followers on new { FollowerId = userId, FollowingId = p.UserId } equals new { f.FollowerId, f.FollowingId } into followings
                        from f in followings.DefaultIfEmpty()
                        where
                            // Include posts from users the current user follows and the user's own posts
                            ((f != null && f.Accepted) || p.UserId == userId) &&
                            // Only include posts that are public or from the user themselves
                            (p.UserId == userId || (!u.IsPrivate && p.Visibility.ToLower() == VisibilityValues.PUBLIC))
                        orderby p.CreatedAt descending
                        select p;

            // Apply pagination
            var posts = await query
                .AsNoTracking()
                .Skip((page - 1) * limit)
                .Take(limit)
                .Include(p => p.User)
                .Include(p => p.Media)
                .Include(p => p.ParentPost)
                .ThenInclude(p => p.ParentPost)
                .ThenInclude(p => p.ParentPost)
                .ThenInclude(p => p.User)
                .ToListAsync();

            // If posts list is empty, return it immediately
            if (!posts.Any())
                return posts;

            // Get post IDs for the efficient likes query
            var postIds = posts.Select(p => p.Id).ToList();

            // Get likes for just these specific posts (instead of all likes)
            var likedPostIds = await _context.Likes
                .AsNoTracking()
                .Where(l => l.UserId == userId && postIds.Contains(l.PostId))
                .Select(l => l.PostId)
                .ToListAsync();

            // Create a HashSet for O(1) lookups
            var likedPostsSet = new HashSet<string>(likedPostIds);

            // Set IsLiked property efficiently
            foreach (var post in posts)
            {
                post.IsLiked = likedPostsSet.Contains(post.Id);
            }

            return posts;
        }

        public async Task<IEnumerable<Post>> GetMyPostsAsync(string userId, int page = 1, int limit = 20)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            if (page < 1)
                throw new ArgumentOutOfRangeException(nameof(page), "Page number must be greater than 0.");

            if (limit < 1)
                throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be greater than 0.");

            // Get paginated posts first
            var posts = await _context.Posts
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Include(p => p.User)
                .Include(p => p.Media)
                .Include(p => p.ParentPost)
                .ThenInclude(p => p.ParentPost)
                .ThenInclude(p => p.ParentPost)
                .ThenInclude(p => p.User)
                .ToListAsync();

            // If posts list is empty, return it immediately
            if (!posts.Any())
                return posts;

            // Get post IDs for the efficient likes query
            var postIds = posts.Select(p => p.Id).ToList();

            // Only query likes for the specific posts we've retrieved
            var likedPostIds = await _context.Likes
                .AsNoTracking()
                .Where(l => l.UserId == userId && postIds.Contains(l.PostId))
                .Select(l => l.PostId)
                .ToListAsync();

            // Create a HashSet for O(1) lookups
            var likedPostsSet = new HashSet<string>(likedPostIds);

            // Set IsLiked property efficiently
            foreach (var post in posts)
            {
                post.IsLiked = likedPostsSet.Contains(post.Id);
            }

            return posts;
        }

        public async Task<Post> GetPostByIdAsync(string postId, string? userId = null)
        {
            var post = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Media)
                .Include(p => p.ParentPost)
                .ThenInclude(p => p.ParentPost)
                .ThenInclude(p => p.ParentPost)
                .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
                throw new Exception("Post not found");

            bool isOwner = userId == post.UserId;

            if (post.User.IsPrivate && !isOwner)
                throw new Exception("You are not allowed to view this private post");

            if (post.Visibility.ToLower() != VisibilityValues.PUBLIC && !isOwner)
                throw new Exception("This post is not public");

            // If userId is provided, check if the post is liked by this user
            if (!string.IsNullOrEmpty(userId))
            {
                var isLiked = await _context.Likes
                    .AsNoTracking()
                    .AnyAsync(l => l.UserId == userId && l.PostId == post.Id);
                post.IsLiked = isLiked;
            }

            return post;
        }

        public async Task<IEnumerable<Post>> GetPostsByUserIdAsync(string userId, int page = 1, int limit = 20, string? myUserId = null)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            if (page < 1)
                throw new ArgumentOutOfRangeException(nameof(page), "Page number must be greater than 0.");

            if (limit < 1)
                throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be greater than 0.");

            // Get paginated posts first
            var posts = await _context.Posts
                .AsNoTracking()
                .Where(p => p.UserId == userId && p.Visibility.Equals(VisibilityValues.PUBLIC))
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Include(p => p.User)
                .Include(p => p.Media)
                .Include(p => p.ParentPost)
                .ThenInclude(p => p.ParentPost)
                .ThenInclude(p => p.ParentPost)
                .ThenInclude(p => p.User)
                .ToListAsync();

            // If posts list is empty, return it immediately
            if (!posts.Any())
                return posts;

            // Get post IDs for the efficient likes query
            var postIds = posts.Select(p => p.Id).ToList();

            // Only query likes for the specific posts we've retrieved
            var likedPostIds = await _context.Likes
                .AsNoTracking()
                .Where(l => l.UserId == myUserId && postIds.Contains(l.PostId))
                .Select(l => l.PostId)
                .ToListAsync();

            // Create a HashSet for O(1) lookups
            var likedPostsSet = new HashSet<string>(likedPostIds);

            // Set IsLiked property efficiently
            foreach (var post in posts)
            {
                post.IsLiked = likedPostsSet.Contains(post.Id);
            }

            return posts;
        }

        public async Task<Post> UpdatePostAsync(Post post)
        {
            var existing = await _context.Posts.FindAsync(post.Id);
            if (existing == null)
                throw new Exception("Post not found");
            if (existing.UserId != post.UserId)
                throw new Exception("You are not allowed to update this post");

            if (post.Title is not null) existing.Title = post.Title;
            if (post.Content is not null) existing.Content = post.Content;
            if (post.Visibility is not null) existing.Visibility = post.Visibility;
            if (post.Media is not null) existing.Media = post.Media;

            existing.UpdatedAt = DateTime.UtcNow;

            _context.Posts.Update(existing);
            await _context.SaveChangesAsync();
            return existing;
        }
    }
}
