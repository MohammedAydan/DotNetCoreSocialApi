using Microsoft.EntityFrameworkCore;
using Social.Core.Interfaces;
using Social.Infrastructure.Data;
using Social.Core.Entities;
using Social.Core;

namespace Social.Infrastructure.Repositories
{
    public class PostRepository(ApplicationDbContext _context) : IPostRepository
    {
        private const int MaxPageLimit = 50;

        public async Task<Post> AddPostAsync(Post post, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(post);
            if (string.IsNullOrWhiteSpace(post.UserId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(post));

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            // Pre-check before Add so a missing user surfaces as KeyNotFoundException (404)
            // instead of a raw FK DbUpdateException (500). The ExecuteUpdate rows guard below
            // is retained as a race guard in case the user is removed concurrently.
            var userExists = await _context.Users.AnyAsync(u => u.Id == post.UserId, cancellationToken);
            if (!userExists)
                throw new KeyNotFoundException($"User '{post.UserId}' not found.");

            post.Id = Guid.NewGuid().ToString();
            post.CreatedAt = DateTime.UtcNow;
            post.UpdatedAt = DateTime.UtcNow;
            post.IsDeleted = false;
            _context.Posts.Add(post);
            await _context.SaveChangesAsync(cancellationToken);

            var rows = await _context.Users
                .Where(u => u.Id == post.UserId)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.PostsCount, u => u.PostsCount + 1), cancellationToken);

            if (rows == 0)
                throw new KeyNotFoundException($"User '{post.UserId}' not found. Post creation rolled back.");

            await transaction.CommitAsync(cancellationToken);
            return post;
        }

        public async Task<Post> SharePostAsync(string postId, Post post, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(postId))
                throw new ArgumentException("Post ID cannot be null or empty.", nameof(postId));
            if (post is null)
                throw new ArgumentNullException(nameof(post));
            if (string.IsNullOrWhiteSpace(post.UserId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(post));

            var currentUserId = post.UserId;

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            // Pre-check sharer existence before Add (see AddPostAsync): explicit 404
            // instead of a raw FK failure, plus a race guard on the counter update below.
            var sharerExists = await _context.Users.AnyAsync(u => u.Id == currentUserId, cancellationToken);
            if (!sharerExists)
                throw new KeyNotFoundException($"User '{currentUserId}' not found.");

            // Strict policy validation (read before write so failures roll back with zero side effects).
            var originalPost = await _context.Posts
                .AsNoTracking()
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == postId, cancellationToken);

            if (originalPost is null || originalPost.IsDeleted)
                throw new KeyNotFoundException("Target post does not exist or has been deleted.");

            if (!string.Equals(originalPost.Visibility, VisibilityValues.Public, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only public posts can be shared.");

            // Null-safe: orphaned post (User navigation missing) cannot prove a public profile.
            var authorId = originalPost.UserId;
            var authorIsPrivate = originalPost.User?.IsPrivate ?? true;
            if (authorIsPrivate)
                throw new InvalidOperationException("Posts from private accounts cannot be shared.");

            var blocked = await _context.BlockUsers.AsNoTracking().AnyAsync(b =>
                (b.UserId == currentUserId && b.BlockedUserId == authorId) ||
                (b.UserId == authorId && b.BlockedUserId == currentUserId), cancellationToken);

            if (blocked)
                throw new InvalidOperationException("Sharing is not allowed between blocked users.");

            post.Id = Guid.NewGuid().ToString();
            post.CreatedAt = DateTime.UtcNow;
            post.UpdatedAt = DateTime.UtcNow;
            post.IsDeleted = false;
            post.ParentPostId = postId;
            _context.Posts.Add(post);
            await _context.SaveChangesAsync(cancellationToken);

            var userRows = await _context.Users
                .Where(u => u.Id == currentUserId)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.PostsCount, u => u.PostsCount + 1), cancellationToken);

            if (userRows == 0)
                throw new KeyNotFoundException($"User '{currentUserId}' not found. Share rolled back.");

            var shareRows = await _context.Posts
                .Where(p => p.Id == postId && !p.IsDeleted)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.ShareingsCount, p => p.ShareingsCount + 1), cancellationToken);

            if (shareRows == 0)
                throw new KeyNotFoundException("Target post no longer exists. Share rolled back.");

            await transaction.CommitAsync(cancellationToken);

            // Decision (Bug 2): do NOT assign the detached originalPost navigation here.
            // The validation copy was loaded AsNoTracking while the context may already track
            // a different instance with the same key (long-lived/reused contexts); attaching it
            // would throw an identity-conflict InvalidOperationException or risk a re-insert.
            // Callers hydrate the parent via the read methods (which Include ParentPost).
            return post;
        }

        /// <summary>
        /// Soft-deletes a post. Policy for child shares of a deleted parent:
        /// shares stay visible (their <c>IsDeleted</c> is untouched and their owners'
        /// <c>PostsCount</c> is not decremented); their <c>ParentPost</c> content is
        /// masked for everyone except the parent's owner (see
        /// <see cref="MaskDeletedParentPosts"/>); the parent's <c>ShareingsCount</c>
        /// is left as-is because the parent is soft-deleted. Child shares are never
        /// hard-deleted here.
        /// </summary>
        public async Task<bool> DeletePostAsync(string postId, string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(postId) || string.IsNullOrWhiteSpace(userId))
                return false;

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var now = DateTime.UtcNow;

            // Soft delete only; never orphan child shares (they keep ParentPostId for timeline integrity).
            // Decision (Bug 8): child shares' UpdatedAt is intentionally NOT bumped — the extra
            // write buys nothing (feed ordering is by the share's own CreatedAt, which is unchanged).
            var postRows = await _context.Posts
                .Where(p => p.Id == postId && p.UserId == userId && !p.IsDeleted)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.IsDeleted, true)
                    .SetProperty(p => p.UpdatedAt, now), cancellationToken);

            if (postRows == 0)
                return false;

            var userRows = await _context.Users
                .Where(u => u.Id == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.PostsCount, u => u.PostsCount > 0 ? u.PostsCount - 1 : 0), cancellationToken);

            if (userRows == 0)
                throw new KeyNotFoundException($"User '{userId}' not found. Delete rolled back.");

            await transaction.CommitAsync(cancellationToken);
            return true;
        }

        public async Task<IEnumerable<Post>> GetFeedPostsAsync(
            string userId,
            int page = 1,
            int limit = 20,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            ValidatePage(page);
            limit = NormalizeLimit(limit);

            // No Users join in the filter: subqueries only, Includes applied after pagination.
            var posts = await LoadCompletePostsQuery(
                    FeedQueryForViewer(userId)
                        .Skip((page - 1) * limit)
                        .Take(limit))
                .ToListAsync(cancellationToken);

            if (posts.Count == 0)
                return posts;

            MaskDeletedParentPosts(posts, userId);
            await PopulateLikesAsync(posts, userId, cancellationToken);
            return posts;
        }

        public async Task<IEnumerable<Post>> GetMyPostsAsync(string userId, int page = 1, int limit = 20, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            ValidatePage(page);
            limit = NormalizeLimit(limit);

            var posts = await LoadCompletePostsQuery(
                    _context.Posts
                        .Where(p => p.UserId == userId && !p.IsDeleted)
                        .OrderByDescending(p => p.CreatedAt)
                        .ThenByDescending(p => p.Id)
                        .Skip((page - 1) * limit)
                        .Take(limit))
                .ToListAsync(cancellationToken);

            if (posts.Count == 0)
                return posts;

            MaskDeletedParentPosts(posts, userId);
            await PopulateLikesAsync(posts, userId, cancellationToken);
            return posts;
        }

        public async Task<Post> GetPostByIdAsync(string postId, string? userId = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(postId))
                throw new ArgumentException("Post ID cannot be null or empty.", nameof(postId));

            var post = await LoadCompletePostsQuery(_context.Posts.Where(p => p.Id == postId))
                .FirstOrDefaultAsync(cancellationToken);

            if (post is null || post.IsDeleted)
                throw new KeyNotFoundException("Post not found.");

            var isOwner = userId is not null && userId == post.UserId;

            // Two-way block enforcement (null-safe on author id).
            if (!string.IsNullOrEmpty(userId) && !isOwner)
            {
                var blocked = await _context.BlockUsers.AsNoTracking().AnyAsync(b =>
                    (b.UserId == userId && b.BlockedUserId == post.UserId) ||
                    (b.UserId == post.UserId && b.BlockedUserId == userId), cancellationToken);

                if (blocked)
                    throw new UnauthorizedAccessException("You are not allowed to view this post.");
            }

            // Null-safe privacy checks: private accounts are visible to owners
            // and accepted followers; all others fail closed.
            if (!isOwner)
            {
                if (post.User?.IsPrivate ?? true)
                {
                    if (string.IsNullOrEmpty(userId))
                        throw new UnauthorizedAccessException("You must log in to view this private post.");

                    var isFollowing = await _context.Followers.AsNoTracking().AnyAsync(f =>
                        f.FollowerId == userId && f.FollowingId == post.UserId && f.Accepted, cancellationToken);

                    if (!isFollowing)
                        throw new UnauthorizedAccessException("This account is private. Follow to view posts.");
                }

                if (!string.Equals(post.Visibility, VisibilityValues.Public, StringComparison.OrdinalIgnoreCase))
                    throw new UnauthorizedAccessException("This post is not public.");
            }

            MaskDeletedParentPosts(new[] { post }, userId);

            if (!string.IsNullOrEmpty(userId))
                await PopulateLikesAsync(new[] { post }, userId, cancellationToken);

            return post;
        }

        public async Task<IEnumerable<Post>> GetPostsByUserIdAsync(string userId, int page = 1, int limit = 20, string? myUserId = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            ValidatePage(page);
            limit = NormalizeLimit(limit);

            var isOwner = !string.IsNullOrEmpty(myUserId) && myUserId == userId;

            // Bidirectional block: blocked users see no content from each other.
            if (!string.IsNullOrEmpty(myUserId) && !isOwner)
            {
                var blocked = await _context.BlockUsers.AsNoTracking().AnyAsync(b =>
                    (b.UserId == myUserId && b.BlockedUserId == userId) ||
                    (b.UserId == userId && b.BlockedUserId == myUserId), cancellationToken);

                if (blocked)
                    return Enumerable.Empty<Post>();
            }

            if (!isOwner)
            {
                var targetUser = await _context.Users
                    .AsNoTracking()
                    .Select(u => new { u.Id, u.IsPrivate })
                    .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

                if (targetUser is null)
                    throw new KeyNotFoundException("User not found.");

                if (targetUser.IsPrivate)
                {
                    var isFollowing = !string.IsNullOrEmpty(myUserId) && await _context.Followers.AsNoTracking().AnyAsync(f =>
                        f.FollowerId == myUserId && f.FollowingId == userId && f.Accepted, cancellationToken);

                    if (!isFollowing)
                        return Enumerable.Empty<Post>();
                }
            }

            // Owners see their own private posts too; everyone else sees public posts only.
            var posts = await LoadCompletePostsQuery(
                    _context.Posts
                        .Where(p => p.UserId == userId
                            && !p.IsDeleted
                            && (isOwner || p.Visibility == VisibilityValues.Public))
                        .OrderByDescending(p => p.CreatedAt)
                        .ThenByDescending(p => p.Id)
                        .Skip((page - 1) * limit)
                        .Take(limit))
                .ToListAsync(cancellationToken);

            if (posts.Count == 0)
                return posts;

            MaskDeletedParentPosts(posts, myUserId);

            if (!string.IsNullOrEmpty(myUserId))
                await PopulateLikesAsync(posts, myUserId, cancellationToken);

            return posts;
        }

        public async Task<Post> UpdatePostAsync(Post post, CancellationToken cancellationToken = default)
        {
            if (post is null)
                throw new ArgumentNullException(nameof(post));
            if (string.IsNullOrWhiteSpace(post.Id))
                throw new ArgumentException("Post ID cannot be null or empty.", nameof(post));

            var existing = await _context.Posts
                .Include(p => p.Media)
                .FirstOrDefaultAsync(p => p.Id == post.Id, cancellationToken);

            if (existing is null || existing.IsDeleted)
                throw new KeyNotFoundException("Post not found.");
            if (existing.UserId != post.UserId)
                throw new UnauthorizedAccessException("You are not allowed to update this post.");

            if (post.Title is not null) existing.Title = post.Title;
            if (post.Content is not null) existing.Content = post.Content;
            if (post.Visibility is not null) existing.Visibility = post.Visibility;
            existing.UpdatedAt = DateTime.UtcNow;

            if (post.Media is not null)
                await ReconcileMediaAsync(existing, post.Media, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            return existing;
        }

        /// <summary>
        /// Builds the visibility-filtered, deterministically-ordered feed base query for a viewer.
        /// Policy: own non-deleted posts always; otherwise public non-deleted posts of accepted
        /// follows, excluding both directions of blocks. No Users join — subqueries only, so the
        /// covering indexes stay effective. Centralizes the predicate (single definition point).
        /// Side effects: none (query composition only, fully translatable).
        /// </summary>
        private IQueryable<Post> FeedQueryForViewer(string viewerId)
        {
            return from p in _context.Posts
                   where !p.IsDeleted
                       && (p.UserId == viewerId
                           || (_context.Followers.Any(f =>
                                   f.FollowerId == viewerId
                                   && f.FollowingId == p.UserId
                                   && f.Accepted)
                               && p.Visibility == VisibilityValues.Public
                               && !_context.BlockUsers.Any(b =>
                                   (b.UserId == viewerId && b.BlockedUserId == p.UserId) ||
                                   (b.UserId == p.UserId && b.BlockedUserId == viewerId))))
                   orderby p.CreatedAt descending, p.Id descending
                   select p;
        }

        /// <summary>
        /// Applies the complete eager-load graph for post reads: author, own media, and the
        /// parent-post chain to a depth of 3 levels (4 posts total including the root), each
        /// level with its author and media. Requires EF Core 5+ for <c>AsSplitQuery</c> (project
        /// targets EF Core 9); split queries prevent Cartesian explosion from the parallel
        /// <c>ParentPost</c> branches. Apply after <c>Skip/Take</c> for paginated reads (so only
        /// the page is hydrated) and directly for single-post reads. Depth matches
        /// <see cref="PopulateLikesAsync"/>, which covers the same 4-post chain.
        /// Side effects: none (query composition only).
        /// </summary>
        private static IQueryable<Post> LoadCompletePostsQuery(IQueryable<Post> query)
        {
            return query
                .AsNoTracking()
                .Include(p => p.User)
                .Include(p => p.Media)
                .Include(p => p.ParentPost).ThenInclude(p => p!.User)
                .Include(p => p.ParentPost).ThenInclude(p => p!.Media)
                .Include(p => p.ParentPost).ThenInclude(p => p!.ParentPost).ThenInclude(p => p!.User)
                .Include(p => p.ParentPost).ThenInclude(p => p!.ParentPost).ThenInclude(p => p!.Media)
                .Include(p => p.ParentPost).ThenInclude(p => p!.ParentPost).ThenInclude(p => p!.ParentPost).ThenInclude(p => p!.User)
                .Include(p => p.ParentPost).ThenInclude(p => p!.ParentPost).ThenInclude(p => p!.ParentPost).ThenInclude(p => p!.Media)
                .AsSplitQuery();
        }

        /// <summary>
        /// Throws unless <paramref name="page"/> is a 1-based page number.
        /// Policy (Bug 5): pagination inputs throw instead of clamping, so client bugs
        /// surface loudly; only the upper <c>limit</c> bound is clamped (see
        /// <see cref="NormalizeLimit"/>). Side effects: none.
        /// </summary>
        private static void ValidatePage(int page)
        {
            if (page < 1)
                throw new ArgumentOutOfRangeException(nameof(page), "Page number must be greater than 0.");
        }

        /// <summary>
        /// Validates <paramref name="limit"/> and caps it at 50. Throws when less than 1;
        /// values above the cap are clamped (not thrown) to protect the server from
        /// oversized pages. Side effects: none.
        /// </summary>
        private static int NormalizeLimit(int limit)
        {
            if (limit < 1)
                throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be greater than 0.");
            return Math.Min(limit, MaxPageLimit);
        }

        /// <summary>
        /// Masks soft-deleted ancestors in already-materialized (<c>AsNoTracking</c>) post graphs.
        /// Walks the full <c>ParentPost</c> chain (not just the direct parent): a deleted ancestor
        /// keeps its identity (<c>Id</c>, <c>UserId</c>, <c>IsDeleted</c>) so timelines stay intact,
        /// but non-owners see placeholder content, no title, and no media. The deleted post's owner
        /// still sees their own content unmasked. There is intentionally no global query filter for
        /// <c>IsDeleted</c> so admin/moderation flows can still list deleted content.
        /// Side effects: mutates the in-memory detached entities only; never writes to the database.
        /// </summary>
        private static void MaskDeletedParentPosts(IEnumerable<Post> posts, string? viewerUserId = null)
        {
            foreach (var post in posts)
            {
                var current = post.ParentPost;
                while (current is not null)
                {
                    if (current.IsDeleted)
                    {
                        var isOwnerOfParent = viewerUserId is not null
                            && viewerUserId == current.UserId;

                        if (!isOwnerOfParent)
                        {
                            current.Content = "[This content has been deleted]";
                            current.Title = null;
                            current.Media = new List<Media>();
                        }
                    }
                    current = current.ParentPost;
                }
            }
        }

        /// <summary>
        /// Reconciles media by Id: deletes removed rows, updates modified rows,
        /// inserts newly uploaded rows. Never matches by URL (URLs are not unique).
        /// Duplicate Ids in either collection are collapsed (first wins) so hostile or
        /// buggy payloads cannot crash dictionary construction. Side effects: stages
        /// add/remove/update operations on <c>_context.Media</c>; caller saves.
        /// </summary>
        private async Task ReconcileMediaAsync(Post existing, ICollection<Media> incoming, CancellationToken cancellationToken)
        {
            var incomingById = incoming
                .Where(m => !string.IsNullOrWhiteSpace(m.Id))
                .DistinctBy(m => m.Id)
                .ToDictionary(m => m.Id, m => m);

            var existingById = existing.Media.DistinctBy(m => m.Id).ToDictionary(m => m.Id, m => m);

            foreach (var current in existingById.Values)
            {
                if (!incomingById.ContainsKey(current.Id))
                    _context.Media.Remove(current);
            }

            foreach (var kv in incomingById)
            {
                if (existingById.TryGetValue(kv.Key, out var current))
                {
                    current.Name = kv.Value.Name ?? current.Name;
                    current.Type = kv.Value.Type ?? current.Type;
                    current.Url = kv.Value.Url ?? current.Url;
                    current.ThumbnailUrl = kv.Value.ThumbnailUrl ?? current.ThumbnailUrl;
                    current.UpdatedAt = DateTime.UtcNow;
                }
            }

            foreach (var m in incoming)
            {
                if (string.IsNullOrWhiteSpace(m.Id) || !existingById.ContainsKey(m.Id))
                {
                    m.Id = string.IsNullOrWhiteSpace(m.Id) ? Guid.NewGuid().ToString() : m.Id;
                    m.PostId = existing.Id;
                    m.UserId = existing.UserId;
                    m.CreatedAt = DateTime.UtcNow;
                    m.UpdatedAt = DateTime.UtcNow;
                    await _context.Media.AddAsync(m, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Single batch likes query (WHERE PostId IN (...)) covering paginated posts
        /// plus parent posts up to 3 levels (4 posts total including the root); maps IsLiked in O(1).
        /// Side effects: mutates the in-memory <c>IsLiked</c> flags (including ancestors) only.
        /// </summary>
        private async Task PopulateLikesAsync(IEnumerable<Post> posts, string userId, CancellationToken cancellationToken)
        {
            var ids = new HashSet<string>();
            foreach (var post in posts)
                CollectPostChainIds(post, ids);

            if (ids.Count == 0 || string.IsNullOrEmpty(userId))
            {
                foreach (var post in posts)
                    SetIsLikedRecursive(post, null);
                return;
            }

            var likedIds = await _context.Likes
                .AsNoTracking()
                .Where(l => l.UserId == userId && ids.Contains(l.PostId))
                .Select(l => l.PostId)
                .ToListAsync(cancellationToken);

            var likedSet = new HashSet<string>(likedIds);
            foreach (var post in posts)
                SetIsLikedRecursive(post, likedSet);
        }

        /// <summary>
        /// Collects a post's Id plus up to 3 ancestor Ids (4 total) into <paramref name="ids"/>.
        /// Iterative (no recursion). Side effects: adds to the supplied set only.
        /// </summary>
        private static void CollectPostChainIds(Post? post, HashSet<string> ids)
        {
            var current = post;
            var level = 0;
            while (current is not null && level < 4 && !string.IsNullOrEmpty(current.Id))
            {
                ids.Add(current.Id);
                current = current.ParentPost;
                level++;
            }
        }

        /// <summary>
        /// Sets <c>IsLiked</c> along a post's ancestor chain (up to 3 levels, 4 posts total)
        /// from a pre-fetched liked-Id set. Iterative (no recursion).
        /// Side effects: mutates in-memory flags only.
        /// </summary>
        private static void SetIsLikedRecursive(Post? post, HashSet<string>? likedSet)
        {
            var current = post;
            var level = 0;
            while (current is not null && level < 4)
            {
                current.IsLiked = likedSet is not null && likedSet.Contains(current.Id);
                current = current.ParentPost;
                level++;
            }
        }
    }
}
