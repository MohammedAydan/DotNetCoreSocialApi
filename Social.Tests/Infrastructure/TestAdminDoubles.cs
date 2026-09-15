using Social.Core.Entities;
using Social.Core.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Social.Tests.Infrastructure
{
    public class TestAuditLogRepository : IAuditLogRepository
    {
        private readonly List<AuditLog> _logs = new();
        private readonly object _lock = new();

        public Task AddAsync(AuditLog log, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                _logs.Add(log);
            }
            return Task.CompletedTask;
        }

        public Task<(List<AuditLog> Items, int TotalCount)> GetPagedAsync(
            int page,
            int pageSize,
            string? actionType = null,
            string? adminId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                var query = _logs.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(actionType))
                {
                    query = query.Where(l => string.Equals(l.ActionType, actionType, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(adminId))
                {
                    query = query.Where(l => string.Equals(l.AdminId, adminId, StringComparison.OrdinalIgnoreCase));
                }

                if (fromDate.HasValue)
                {
                    query = query.Where(l => l.TimestampUtc >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(l => l.TimestampUtc <= toDate.Value);
                }

                var totalCount = query.Count();
                var items = query
                    .OrderByDescending(l => l.TimestampUtc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Task.FromResult((items, totalCount));
            }
        }
    }

    public class TestAdminRepository : IAdminRepository
    {
        private readonly ConcurrentDictionary<string, User> _users = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, List<string>> _userRoles = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, Post> _posts = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, Comment> _comments = new(StringComparer.OrdinalIgnoreCase);

        public TestAdminRepository()
        {
            SeedInitialData();
        }

        private void SeedInitialData()
        {
            // Seed default users
            AddUser("target-user-001", "targetuser1", "target1@example.com", new List<string> { "User" }, isLocked: true);
            AddUser("active-user-001", "activeuser1", "active1@example.com", new List<string> { "User" });
            AddUser("target-user-to-ban-001", "banneduser", "banneduser@example.com", new List<string> { "User" });
            AddUser("target-user-token-revocation", "revokeduser", "revoked@example.com", new List<string> { "User" });
            AddUser("audited-target-user-001", "auditedtarget", "auditedtarget@example.com", new List<string> { "User" });
            AddUser("promoted-user-001", "promoteduser", "promoted@example.com", new List<string> { "User" });
            AddUser("abusive-author-001", "abusiveauthor", "abusive@example.com", new List<string> { "User" });
            AddUser("trusted-user-001", "trusteduser", "trusted@example.com", new List<string> { "User" });
            AddUser("admin-self-id", "adminself", "adminself@example.com", new List<string> { "Admin" });
            AddUser("admin-user-001", "adminuser", "admin@example.com", new List<string> { "Admin" });
            AddUser("admin-user-id", "adminid", "adminid@example.com", new List<string> { "Admin" });
            AddUser("regular-user-id", "regularuser", "regular@example.com", new List<string> { "User" });

            // Seed default posts
            AddPost("post-001", "target-user-001", "First post content", false);
            AddPost("active-post", "target-user-001", "Active post content", false);
            AddPost("already-hidden-post", "target-user-001", "Hidden post content", true);
            AddPost("post-with-zero-author-count", "target-user-001", "Zero count post", false, authorPostsCount: 0);
            AddPost("post-to-hide-and-restore", "target-user-001", "Hide and restore post", false);
            AddPost("abusive-post-001", "abusive-author-001", "Abusive post content", false);

            // Seed default comments
            AddComment("comment-001", "post-001", "target-user-001", "First comment", false);
            AddComment("comment-with-zero-post-count", "post-001", "target-user-001", "Zero post comment", false);
            AddComment("comment-sync-test-001", "post-001", "target-user-001", "Comment sync test", false);
            AddComment("spam-comment-001", "post-001", "target-user-001", "Spam link comment", false);
        }

        private void AddUser(string id, string userName, string email, List<string> roles, bool isLocked = false)
        {
            _users[id] = new User
            {
                Id = id,
                UserName = userName,
                Email = email,
                FirstName = userName,
                LastName = "User",
                IsVerified = false,
                LockoutEnabled = isLocked,
                LockoutEnd = isLocked ? DateTimeOffset.UtcNow.AddDays(7) : null,
                PostsCount = 1,
                FollowersCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _userRoles[id] = new List<string>(roles);
        }

        private void AddPost(string id, string userId, string content, bool isDeleted, int authorPostsCount = 1)
        {
            var user = _users.TryGetValue(userId, out var u) ? u : new User { Id = userId, UserName = "User" + userId, PostsCount = authorPostsCount };
            user.PostsCount = authorPostsCount;
            _posts[id] = new Post
            {
                Id = id,
                UserId = userId,
                User = user,
                Content = content,
                IsDeleted = isDeleted,
                CreatedAt = DateTime.UtcNow
            };
        }

        private void AddComment(string id, string postId, string userId, string content, bool isDeleted)
        {
            var post = _posts.TryGetValue(postId, out var p) ? p : new Post { Id = postId, CommentsCount = 1 };
            _comments[id] = new Comment
            {
                Id = id,
                PostId = postId,
                Post = post,
                UserId = userId,
                User = _users.TryGetValue(userId, out var u) ? u : new User { Id = userId, UserName = "User" + userId },
                Content = content,
                IsDeleted = isDeleted,
                CreatedAt = DateTime.UtcNow
            };
        }

        public Task<(List<User> Users, int TotalCount)> GetUsersPagedAsync(int page, int pageSize, string? q = null, CancellationToken cancellationToken = default)
        {
            var query = _users.Values.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var trimmed = q.Trim();
                query = query.Where(u =>
                    (u.UserName != null && u.UserName.Contains(trimmed, StringComparison.OrdinalIgnoreCase)) ||
                    (u.Email != null && u.Email.Contains(trimmed, StringComparison.OrdinalIgnoreCase)));
            }

            var total = query.Count();
            var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult((items, total));
        }

        public Task<User?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId) || userId.Contains("non-existent", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<User?>(null);
            }

            if (_users.TryGetValue(userId, out var user))
            {
                return Task.FromResult<User?>(user);
            }

            // Dynamically seed valid user if not already present
            var newUser = new User
            {
                Id = userId,
                UserName = "user_" + userId,
                Email = $"{userId}@example.com",
                FirstName = "Test",
                LastName = "User",
                LockoutEnabled = false,
                LockoutEnd = null,
                CreatedAt = DateTime.UtcNow
            };
            _users[userId] = newUser;
            _userRoles[userId] = new List<string> { "User" };
            return Task.FromResult<User?>(newUser);
        }

        public Task<IList<string>> GetUserRolesAsync(User user, CancellationToken cancellationToken = default)
        {
            if (_userRoles.TryGetValue(user.Id, out var roles))
            {
                return Task.FromResult<IList<string>>(new List<string>(roles));
            }
            return Task.FromResult<IList<string>>(new List<string> { "User" });
        }

        public Task<bool> LockUserAsync(string userId, int? durationDays, string reason, CancellationToken cancellationToken = default)
        {
            if (_users.TryGetValue(userId, out var user))
            {
                user.LockoutEnabled = true;
                user.LockoutEnd = durationDays.HasValue ? DateTimeOffset.UtcNow.AddDays(durationDays.Value) : DateTimeOffset.UtcNow.AddYears(100);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<bool> UnlockUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (_users.TryGetValue(userId, out var user))
            {
                user.LockoutEnd = null;
                user.LockoutEnabled = false;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<bool> UpdateUserRolesAsync(string userId, List<string> roles, CancellationToken cancellationToken = default)
        {
            _userRoles[userId] = new List<string>(roles);
            return Task.FromResult(true);
        }

        public Task<bool> ToggleUserVerificationAsync(string userId, bool isVerified, CancellationToken cancellationToken = default)
        {
            if (_users.TryGetValue(userId, out var user))
            {
                user.IsVerified = isVerified;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<bool> ResetUserPasswordAsync(string userId, string newPassword, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.ContainsKey(userId));
        }

        public Task RevokeUserTokensAsync(string userId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<(List<ModerationFeedItemRecord> Items, int TotalCount)> GetModerationFeedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var postItems = _posts.Values.Select(p => new ModerationFeedItemRecord(
                p.Id, "Post", p.Content ?? "", p.UserId, p.User?.UserName ?? "author", p.IsDeleted, p.LikesCount, p.CommentsCount, p.CreatedAt));
            var commentItems = _comments.Values.Select(c => new ModerationFeedItemRecord(
                c.Id, "Comment", c.Content ?? "", c.UserId, c.User?.UserName ?? "commenter", c.IsDeleted, 0, 0, c.CreatedAt));

            var combined = postItems.Concat(commentItems).OrderByDescending(x => x.CreatedAt).ToList();
            var total = combined.Count;
            var items = combined.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult((items, total));
        }

        public Task<Post?> GetPostByIdAsync(string postId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(postId) || postId.Contains("non-existent", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<Post?>(null);
            }

            if (_posts.TryGetValue(postId, out var post))
            {
                return Task.FromResult<Post?>(post);
            }

            var newPost = new Post
            {
                Id = postId,
                UserId = "target-user-001",
                Content = "Dynamic post content",
                IsDeleted = false,
                User = _users.TryGetValue("target-user-001", out var u) ? u : new User { Id = "target-user-001", PostsCount = 1 }
            };
            _posts[postId] = newPost;
            return Task.FromResult<Post?>(newPost);
        }

        public Task<bool> HidePostAsync(string postId, CancellationToken cancellationToken = default)
        {
            if (_posts.TryGetValue(postId, out var post))
            {
                post.IsDeleted = true;
                if (post.User != null)
                {
                    post.User.PostsCount = Math.Max(0, post.User.PostsCount - 1);
                }
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<bool> RestorePostAsync(string postId, CancellationToken cancellationToken = default)
        {
            if (_posts.TryGetValue(postId, out var post))
            {
                post.IsDeleted = false;
                if (post.User != null)
                {
                    post.User.PostsCount++;
                }
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<bool> UpdatePostVisibilityAsync(string postId, string visibility, CancellationToken cancellationToken = default)
        {
            if (_posts.TryGetValue(postId, out var post))
            {
                post.Visibility = visibility;
                post.UpdatedAt = DateTime.UtcNow;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<bool> DeletePostPermanentlyAsync(string postId, CancellationToken cancellationToken = default)
        {
            if (_posts.TryGetValue(postId, out var post))
            {
                if (post.User != null && !post.IsDeleted)
                {
                    post.User.PostsCount = Math.Max(0, post.User.PostsCount - 1);
                }
                _posts.Remove(postId, out _);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<Comment?> GetCommentByIdAsync(string commentId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(commentId) || commentId.Contains("non-existent", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<Comment?>(null);
            }

            if (_comments.TryGetValue(commentId, out var comment))
            {
                return Task.FromResult<Comment?>(comment);
            }

            var newComment = new Comment
            {
                Id = commentId,
                PostId = "post-001",
                UserId = "target-user-001",
                Content = "Dynamic comment content",
                IsDeleted = false,
                Post = new Post { Id = "post-001", CommentsCount = 1 }
            };
            _comments[commentId] = newComment;
            return Task.FromResult<Comment?>(newComment);
        }

        public Task<bool> HideCommentAsync(string commentId, CancellationToken cancellationToken = default)
        {
            if (_comments.TryGetValue(commentId, out var comment))
            {
                comment.IsDeleted = true;
                if (comment.Post != null)
                {
                    comment.Post.CommentsCount = Math.Max(0, comment.Post.CommentsCount - 1);
                }
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<bool> RestoreCommentAsync(string commentId, CancellationToken cancellationToken = default)
        {
            if (_comments.TryGetValue(commentId, out var comment))
            {
                comment.IsDeleted = false;
                if (comment.Post != null)
                {
                    comment.Post.CommentsCount++;
                }
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<(int TotalUsers, int ActiveUsers24h, int TotalPosts, int TotalComments, int TotalLikes)> GetOverviewMetricsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult((
                TotalUsers: _users.Count,
                ActiveUsers24h: _users.Count,
                TotalPosts: _posts.Count(p => !p.Value.IsDeleted),
                TotalComments: _comments.Count(c => !c.Value.IsDeleted),
                TotalLikes: 42
            ));
        }
    }
}
