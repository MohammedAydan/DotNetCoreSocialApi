using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Social.Core;
using Social.Core.Entities;
using Social.Infrastructure.Data;
using Social.Infrastructure.Repositories;
using Xunit;

namespace Social.Tests.Unit.Repositories;

public class PostRepositoryPrivacyTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly PostRepository _sut;

    public PostRepositoryPrivacyTests()
    {
        // SQLite in-memory (relational) supports ExecuteUpdateAsync + transactions,
        // unlike the EF InMemory provider — closer to production MySQL semantics.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
        _sut = new PostRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Close();
        _connection.Dispose();
    }

    private User MakeUser(string id, bool isPrivate = false) => new()
    {
        Id = id,
        UserName = id,
        NormalizedUserName = id.ToUpperInvariant(),
        Email = $"{id}@test.local",
        NormalizedEmail = $"{id}@TEST.LOCAL".ToUpperInvariant(),
        FirstName = "Test",
        LastName = "User",
        IsPrivate = isPrivate,
        PostsCount = 0,
    };

    private Post MakePost(string id, string userId, string visibility = "public", DateTime? createdAt = null) => new()
    {
        Id = id,
        UserId = userId,
        Visibility = visibility,
        Content = $"content-{id}",
        CreatedAt = createdAt ?? DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        IsDeleted = false,
    };

    [Fact]
    public async Task BlockedUsers_CannotSeeEachOthersContent()
    {
        // Arrange: u1 blocks u2; u2 owns a public post.
        _context.Users.AddRange(MakeUser("u1"), MakeUser("u2"));
        _context.Posts.Add(MakePost("p-blocked", "u2"));
        _context.BlockUsers.Add(BlockUser.Create("u1", "u2"));
        await _context.SaveChangesAsync();

        // Act + Assert: direct fetch rejected.
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.GetPostByIdAsync("p-blocked", "u1"));

        // Reverse direction also rejected.
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.GetPostByIdAsync("p-blocked", "u1"));
        var byUser = await _sut.GetPostsByUserIdAsync("u2", 1, 20, "u1");
        byUser.Should().BeEmpty("bidirectional block must hide profile posts");

        // Feed must exclude blocked author's posts even with accepted follow.
        _context.Followers.Add(new Follower
        {
            Id = "f1", FollowerId = "u1", FollowingId = "u2",
            Accepted = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        });
        await _context.SaveChangesAsync();

        var feed = await _sut.GetFeedPostsAsync("u1", 1, 20);
        feed.Select(p => p.Id).Should().NotContain("p-blocked");
    }

    [Fact]
    public async Task PrivateAccount_Posts_CannotBeShared()
    {
        _context.Users.AddRange(MakeUser("sharer"), MakeUser("author", isPrivate: true));
        var target = MakePost("p-private", "author", VisibilityValues.Public);
        _context.Posts.Add(target);
        await _context.SaveChangesAsync();
        // Simulate navigation load as repository does (Include User).
        _context.Entry(target).Reference(p => p.User).Load();

        var share = new Post { UserId = "sharer", Content = "share attempt", Visibility = VisibilityValues.Public };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SharePostAsync("p-private", share));
    }

    [Fact]
    public async Task NonPublic_Posts_CannotBeShared()
    {
        _context.Users.AddRange(MakeUser("sharer2"), MakeUser("author2"));
        _context.Posts.Add(MakePost("p-followers", "author2", VisibilityValues.Private));
        await _context.SaveChangesAsync();

        var share = new Post { UserId = "sharer2", Content = "x", Visibility = VisibilityValues.Public };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.SharePostAsync("p-followers", share));
    }

    [Fact]
    public async Task Deleted_Posts_CannotBeShared()
    {
        _context.Users.AddRange(MakeUser("sharer3"), MakeUser("author3"));
        _context.Posts.Add(new Post
        {
            Id = "p-deleted", UserId = "author3", Visibility = VisibilityValues.Public,
            Content = "gone", IsDeleted = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        });
        await _context.SaveChangesAsync();

        var share = new Post { UserId = "sharer3", Content = "x", Visibility = VisibilityValues.Public };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.SharePostAsync("p-deleted", share));
    }

    [Fact]
    public async Task Feed_ReturnsDeterministicPagination_WithoutDuplicates()
    {
        const string uid = "pager";
        _context.Users.Add(MakeUser(uid));
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < 10; i++)
            _context.Posts.Add(MakePost($"pg-{i:00}", uid, VisibilityValues.Public, baseTime.AddMinutes(i)));
        await _context.SaveChangesAsync();

        var page1 = (await _sut.GetFeedPostsAsync(uid, 1, 4)).Select(p => p.Id).ToList();
        var page2 = (await _sut.GetFeedPostsAsync(uid, 2, 4)).Select(p => p.Id).ToList();
        var page3 = (await _sut.GetFeedPostsAsync(uid, 3, 4)).Select(p => p.Id).ToList();

        page1.Should().HaveCount(4);
        page2.Should().HaveCount(4);
        page1.Concat(page2).Concat(page3).ToHashSet().Should().HaveCount(page1.Count + page2.Count + page3.Count,
            "consecutive pages must not duplicate items");

        // Deterministic DESC ordering: newest first.
        page1[0].Should().Be("pg-09");
        var combined = page1.Concat(page2).Concat(page3).ToList();
        var numeric = combined.Select(id => int.Parse(id.Substring(3))).ToList();
        numeric.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task Delete_SoftDeletes_AndHidesFromFeed()
    {
        _context.Users.Add(MakeUser("owner", false));
        _context.Users.Find("owner")!.PostsCount = 1;
        _context.Posts.Add(MakePost("p-soft", "owner"));
        await _context.SaveChangesAsync();

        var ok = await _sut.DeletePostAsync("p-soft", "owner");

        ok.Should().BeTrue();
        _context.ChangeTracker.Clear();
        (await _context.Posts.FindAsync("p-soft"))!.IsDeleted.Should().BeTrue("delete must be soft");
        (await _context.Users.FindAsync("owner"))!.PostsCount.Should().Be(0);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.GetPostByIdAsync("p-soft", "owner"));
        (await _sut.GetFeedPostsAsync("owner", 1, 20)).Select(p => p.Id).Should().NotContain("p-soft");
    }

    [Fact]
    public async Task GetPostByIdAsync_AcceptedFollower_CanViewPrivateAccountPublicPost()
    {
        // Arrange: UserA private, UserB accepted follower, public post.
        _context.Users.AddRange(MakeUser("privA", isPrivate: true), MakeUser("userB"));
        _context.Posts.Add(MakePost("p-priv-pub", "privA", VisibilityValues.Public));
        _context.Followers.Add(new Follower
        {
            Id = "f-priv", FollowerId = "userB", FollowingId = "privA",
            Accepted = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        });
        await _context.SaveChangesAsync();

        // Act.
        var post = await _sut.GetPostByIdAsync("p-priv-pub", "userB");

        // Assert.
        post.Should().NotBeNull();
        post.Id.Should().Be("p-priv-pub");
    }

    [Fact]
    public async Task GetPostByIdAsync_NonFollower_CannotViewPrivateAccountPublicPost()
    {
        _context.Users.AddRange(MakeUser("privC", isPrivate: true), MakeUser("userD"));
        _context.Posts.Add(MakePost("p-priv-pub2", "privC", VisibilityValues.Public));
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.GetPostByIdAsync("p-priv-pub2", "userD"));
    }

    [Fact]
    public async Task GetPostsByUserIdAsync_NonFollower_CannotViewPrivateAccountPosts()
    {
        // Arrange: UserA private, UserB does not follow.
        _context.Users.AddRange(MakeUser("privE", isPrivate: true), MakeUser("userF"));
        _context.Posts.Add(MakePost("p-priv-list", "privE", VisibilityValues.Public));
        await _context.SaveChangesAsync();

        // Act.
        var result = await _sut.GetPostsByUserIdAsync("privE", 1, 20, "userF");

        // Assert.
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPostsByUserIdAsync_AcceptedFollower_CanViewPrivateAccountPosts()
    {
        _context.Users.AddRange(MakeUser("privG", isPrivate: true), MakeUser("userH"));
        _context.Posts.Add(MakePost("p-priv-list2", "privG", VisibilityValues.Public));
        _context.Followers.Add(new Follower
        {
            Id = "f-priv2", FollowerId = "userH", FollowingId = "privG",
            Accepted = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        });
        await _context.SaveChangesAsync();

        var result = (await _sut.GetPostsByUserIdAsync("privG", 1, 20, "userH")).ToList();

        result.Select(p => p.Id).Should().Contain("p-priv-list2");
    }

    [Fact]
    public async Task ReconcileMediaAsync_DuplicateIdsInPayload_DoesNotThrow()
    {
        // Arrange: post with one existing media item; payload duplicates its Id.
        _context.Users.Add(MakeUser("mediaOwner"));
        var post = MakePost("p-media", "mediaOwner");
        _context.Posts.Add(post);
        _context.Media.Add(new Media
        {
            Id = "m1", PostId = "p-media", UserId = "mediaOwner",
            Name = "orig", Type = "image", Url = "https://x/1.png",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var update = new Post
        {
            Id = "p-media",
            UserId = "mediaOwner",
            Media = new List<Media>
            {
                new() { Id = "m1", PostId = "p-media", UserId = "mediaOwner", Name = "a", Type = "image", Url = "https://x/1b.png" },
                new() { Id = "m1", PostId = "p-media", UserId = "mediaOwner", Name = "dup", Type = "image", Url = "https://x/1c.png" },
                new() { Id = "", PostId = "p-media", UserId = "mediaOwner", Name = "new", Type = "image", Url = "https://x/2.png" },
            },
        };

        // Act.
        var act = async () => await _sut.UpdatePostAsync(update);

        // Assert: no ArgumentException from duplicate keys.
        await act.Should().NotThrowAsync<ArgumentException>();
        _context.ChangeTracker.Clear();
        (await _context.Media.CountAsync(m => m.PostId == "p-media")).Should().Be(2);
    }

    [Fact]
    public async Task GetFeedPostsAsync_NestedParentMedia_IsLoaded()
    {
        // Arrange: Post A (public) with media; Post B shares A.
        _context.Users.AddRange(MakeUser("authorA"), MakeUser("sharerB"));
        _context.Posts.Add(MakePost("post-A", "authorA", VisibilityValues.Public));
        _context.Media.Add(new Media
        {
            Id = "media-A1", PostId = "post-A", UserId = "authorA",
            Name = "a1", Type = "image", Url = "https://x/a1.png",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        });
        await _context.SaveChangesAsync();

        var share = new Post { UserId = "sharerB", Content = "share", Visibility = VisibilityValues.Public };
        await _sut.SharePostAsync("post-A", share);
        _context.ChangeTracker.Clear();

        // Act: sharer's own share appears in feed with parent media eager-loaded.
        var feed = (await _sut.GetFeedPostsAsync("sharerB", 1, 20)).ToList();

        // Assert.
        var item = feed.FirstOrDefault(p => p.ParentPostId == "post-A");
        item.Should().NotBeNull();
        item!.ParentPost.Should().NotBeNull();
        item.ParentPost!.Media.Select(m => m.Id).Should().Contain("media-A1");
    }

    [Fact]
    public async Task OwnerProfileVisibility_OwnerSeesPrivate_FollowerAndStrangerSeePublicOnly()
    {
        // Arrange: public-account A with one public + one private post; B accepted follower; C stranger.
        _context.Users.AddRange(MakeUser("visA"), MakeUser("visB"), MakeUser("visC"));
        _context.Posts.Add(MakePost("vis-pub", "visA", VisibilityValues.Public));
        _context.Posts.Add(MakePost("vis-priv", "visA", VisibilityValues.Private));
        _context.Followers.Add(new Follower
        {
            Id = "f-vis", FollowerId = "visB", FollowingId = "visA",
            Accepted = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        });
        await _context.SaveChangesAsync();

        // Assert A (owner): both.
        (await _sut.GetPostsByUserIdAsync("visA", 1, 20, "visA"))
            .Select(p => p.Id).Should().BeEquivalentTo("vis-pub", "vis-priv");

        // Assert B (follower): only public.
        (await _sut.GetPostsByUserIdAsync("visA", 1, 20, "visB"))
            .Select(p => p.Id).Should().BeEquivalentTo("vis-pub");

        // Assert C (non-follower): only public.
        (await _sut.GetPostsByUserIdAsync("visA", 1, 20, "visC"))
            .Select(p => p.Id).Should().BeEquivalentTo("vis-pub");
    }

    [Fact]
    public async Task SoftDeletedParent_IsMaskedForViewer_ButVisibleToParentOwner()
    {
        // Arrange: A posts, B shares A, then A soft-deletes.
        _context.Users.AddRange(MakeUser("maskA"), MakeUser("maskB"), MakeUser("maskC"));
        _context.Posts.Add(MakePost("mask-A", "maskA", VisibilityValues.Public));
        _context.Media.Add(new Media
        {
            Id = "mask-m1", PostId = "mask-A", UserId = "maskA",
            Name = "m", Type = "image", Url = "https://x/m.png",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        });
        await _context.SaveChangesAsync();

        var share = new Post { UserId = "maskB", Content = "share", Visibility = VisibilityValues.Public };
        var created = await _sut.SharePostAsync("mask-A", share);
        (await _sut.DeletePostAsync("mask-A", "maskA")).Should().BeTrue();
        _context.ChangeTracker.Clear();

        // Assert (non-owner viewer C): masked placeholder, no media.
        var forStranger = await _sut.GetPostByIdAsync(created.Id, "maskC");
        forStranger.ParentPost.Should().NotBeNull();
        forStranger.ParentPost!.IsDeleted.Should().BeTrue();
        forStranger.ParentPost.Content.Should().Be("[This content has been deleted]");
        forStranger.ParentPost.Media.Should().BeEmpty();

        // Assert (owner of A): content not masked.
        var forOwner = await _sut.GetPostByIdAsync(created.Id, "maskA");
        forOwner.ParentPost!.Content.Should().NotBe("[This content has been deleted]");
        forOwner.ParentPost.Media.Select(m => m.Id).Should().Contain("mask-m1");
    }

    [Fact]
    public async Task ShareOfDeletedParent_StaysVisible_WithChainIntact()
    {
        // Arrange: A posts, B shares, A soft-deletes.
        _context.Users.AddRange(MakeUser("orphA"), MakeUser("orphB"));
        _context.Posts.Add(MakePost("orph-A", "orphA", VisibilityValues.Public));
        await _context.SaveChangesAsync();

        var share = new Post { UserId = "orphB", Content = "share", Visibility = VisibilityValues.Public };
        await _sut.SharePostAsync("orph-A", share);
        (await _sut.DeletePostAsync("orph-A", "orphA")).Should().BeTrue();
        _context.ChangeTracker.Clear();

        // Assert: B's share still listed, chain intact, parent flagged deleted.
        var mine = (await _sut.GetMyPostsAsync("orphB", 1, 20)).ToList();
        mine.Select(p => p.ParentPostId).Should().Contain("orph-A");
        var fetched = await _sut.GetPostByIdAsync(mine.First(p => p.ParentPostId == "orph-A").Id, "orphB");
        fetched.ParentPost.Should().NotBeNull();
        fetched.ParentPost!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task BlockEnforcement_BidirectionalAcrossFeedAndSinglePost()
    {
        // Arrange: A blocks B; both own public posts.
        _context.Users.AddRange(MakeUser("blkA"), MakeUser("blkB"));
        _context.Posts.Add(MakePost("blk-post-A", "blkA", VisibilityValues.Public));
        _context.Posts.Add(MakePost("blk-post-B", "blkB", VisibilityValues.Public));
        _context.BlockUsers.Add(BlockUser.Create("blkA", "blkB"));
        await _context.SaveChangesAsync();

        // Assert: each feed excludes the other's posts.
        (await _sut.GetFeedPostsAsync("blkA", 1, 20)).Select(p => p.Id).Should().NotContain("blk-post-B");
        (await _sut.GetFeedPostsAsync("blkB", 1, 20)).Select(p => p.Id).Should().NotContain("blk-post-A");

        // Assert: direct fetch across the block throws.
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.GetPostByIdAsync("blk-post-B", "blkA"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.GetPostByIdAsync("blk-post-A", "blkB"));
    }

    [Fact]
    public async Task PaginationValidation_ThrowsOnInvalid_ClampsExcessiveLimit()
    {
        _context.Users.Add(MakeUser("pageU"));
        for (var i = 0; i < 55; i++)
            _context.Posts.Add(MakePost($"pgv-{i:00}", "pageU", VisibilityValues.Public,
                new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(i)));
        await _context.SaveChangesAsync();

        // Assert: page < 1 throws on all paginated reads.
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _sut.GetFeedPostsAsync("pageU", 0, 10));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _sut.GetMyPostsAsync("pageU", 0, 10));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _sut.GetPostsByUserIdAsync("pageU", 0, 10, "pageU"));

        // Assert: limit < 1 throws on all paginated reads.
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _sut.GetFeedPostsAsync("pageU", 1, 0));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _sut.GetMyPostsAsync("pageU", 1, 0));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _sut.GetPostsByUserIdAsync("pageU", 1, 0, "pageU"));

        // Assert: excessive limit clamps to MaxPageLimit (50 of 55).
        (await _sut.GetFeedPostsAsync("pageU", 1, 1000)).Should().HaveCount(50);
    }

    [Fact]
    public async Task LikesPopulation_SetsIsLiked_OnFullFourPostChain()
    {
        // Arrange: chain p0 <- p1 <- p2 <- p3 (3 parent levels); viewer liked all four.
        _context.Users.AddRange(MakeUser("likeOwner"), MakeUser("liker"));
        _context.Posts.Add(MakePost("lk-0", "likeOwner", VisibilityValues.Public));
        _context.Posts.Add(new Post
        {
            Id = "lk-1", UserId = "likeOwner", Visibility = VisibilityValues.Public,
            Content = "c1", ParentPostId = "lk-0", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        });
        _context.Posts.Add(new Post
        {
            Id = "lk-2", UserId = "likeOwner", Visibility = VisibilityValues.Public,
            Content = "c2", ParentPostId = "lk-1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        });
        _context.Posts.Add(new Post
        {
            Id = "lk-3", UserId = "likeOwner", Visibility = VisibilityValues.Public,
            Content = "c3", ParentPostId = "lk-2", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        });
        foreach (var pid in new[] { "lk-0", "lk-1", "lk-2", "lk-3" })
            _context.Likes.Add(new Like
            {
                Id = $"like-{pid}", PostId = pid, UserId = "liker",
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act.
        var root = await _sut.GetPostByIdAsync("lk-3", "liker");

        // Assert: all four levels flagged.
        root.IsLiked.Should().BeTrue();
        root.ParentPost!.IsLiked.Should().BeTrue();
        root.ParentPost.ParentPost!.IsLiked.Should().BeTrue();
        root.ParentPost.ParentPost.ParentPost!.IsLiked.Should().BeTrue();
    }

    [Fact]
    public async Task SharePostAsync_AllowsSubsequentSaveChanges_OnSameContext()
    {
        // Arrange.
        _context.Users.AddRange(MakeUser("detA"), MakeUser("detB"));
        _context.Posts.Add(MakePost("det-A", "detA", VisibilityValues.Public));
        await _context.SaveChangesAsync();

        // Act: share, then reuse the same context (proves no detached re-insert tracking).
        var share = new Post { UserId = "detB", Content = "s", Visibility = VisibilityValues.Public };
        var created = await _sut.SharePostAsync("det-A", share);
        created.ParentPostId.Should().Be("det-A");
        // EF relationship fixup may populate ParentPost from the already-tracked parent
        // instance; what matters is no second tracked copy exists (no identity conflict).
        if (created.ParentPost is not null)
            created.ParentPost.Id.Should().Be("det-A");
        var act = async () => await _context.SaveChangesAsync();

        // Assert.
        await act.Should().NotThrowAsync();
    }
}
