using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Social.Core;
using Social.Core.Entities;
using Social.Infrastructure.Data;
using Social.Infrastructure.Repositories;
using Xunit;

namespace Social.Tests.Unit.Notifications
{
    public class NotificationIntelligenceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ApplicationDbContext _context;
        private readonly NotificationRepository _sut;

        public NotificationIntelligenceTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;
            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();
            _sut = new NotificationRepository(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Close();
            _connection.Dispose();
        }

        private static User MakeUser(string id, string? userName = null) => new()
        {
            Id = id,
            UserName = userName ?? id,
            NormalizedUserName = (userName ?? id).ToUpperInvariant(),
            Email = $"{id}@test.local",
            NormalizedEmail = $"{id}@TEST.LOCAL".ToUpperInvariant(),
            FirstName = "Test",
            LastName = "User",
            PostsCount = 0,
        };

        private static Notification Like(string recipientId, string likerId, string postId, string? actorName = null) => new()
        {
            Id = Guid.NewGuid().ToString(),
            RecipientId = recipientId,
            UserId = likerId,
            Type = NotificationActionTypes.Like,
            Message = $"{likerId} liked your post.",
            PostId = postId,
            LastActorName = actorName,
            CreatedAt = DateTime.UtcNow
        };

        [Fact]
        public async Task Likes_OnSamePost_AggregateIntoOneRow()
        {
            _context.Users.AddRange(MakeUser("author"), MakeUser("l1", "alice"), MakeUser("l2", "bob"));
            await _context.SaveChangesAsync();

            await _sut.AddAsync(Like("author", "l1", "p1", "alice"));
            await _sut.AddAsync(Like("author", "l2", "p1", "bob"));

            var rows = await _sut.GetByUserIdAsync("author");
            rows.Should().ContainSingle();
            var single = rows.Single();
            single.ActorCount.Should().Be(2);
            single.LastActorName.Should().Be("bob");
            single.Message.Should().Contain("and 1 other");
            single.GroupKey.Should().Be("like:post:p1");
            single.Priority.Should().Be(NotificationPriority.Like);
        }

        [Fact]
        public async Task Likes_OnDifferentPosts_StaySeparate()
        {
            _context.Users.AddRange(MakeUser("author"), MakeUser("l1", "alice"));
            await _context.SaveChangesAsync();

            await _sut.AddAsync(Like("author", "l1", "p1", "alice"));
            await _sut.AddAsync(Like("author", "l1", "p2", "alice"));

            (await _sut.GetByUserIdAsync("author")).Should().HaveCount(2);
        }

        [Fact]
        public async Task DisabledType_IsSkipped()
        {
            _context.Users.AddRange(MakeUser("author"), MakeUser("l1", "alice"));
            _context.NotificationPreferences.Add(new NotificationPreference { UserId = "author", LikeEnabled = false });
            await _context.SaveChangesAsync();

            await _sut.AddAsync(Like("author", "l1", "p1", "alice"));

            (await _sut.GetByUserIdAsync("author")).Should().BeEmpty();
        }

        [Fact]
        public async Task Moderation_BypassesTypeToggle()
        {
            _context.Users.AddRange(MakeUser("author"), MakeUser("admin", "root"));
            _context.NotificationPreferences.Add(new NotificationPreference { UserId = "author", LikeEnabled = false });
            await _context.SaveChangesAsync();

            await _sut.AddAsync(new Notification
            {
                Id = Guid.NewGuid().ToString(),
                RecipientId = "author",
                UserId = "admin",
                Type = "ModerationNotice",
                Message = "Your post was hidden.",
                CreatedAt = DateTime.UtcNow
            });

            var rows = await _sut.GetByUserIdAsync("author");
            rows.Should().ContainSingle();
            rows.Single().Priority.Should().Be(NotificationPriority.Moderation);
        }

        [Fact]
        public async Task QuietHours_DeferAndRelease()
        {
            var now = DateTime.UtcNow;
            _context.Users.AddRange(MakeUser("author"), MakeUser("l1", "alice"));
            _context.NotificationPreferences.Add(new NotificationPreference
            {
                UserId = "author",
                QuietStartHourUtc = (now.Hour + 23) % 24,
                QuietEndHourUtc = (now.Hour + 1) % 24
            });
            await _context.SaveChangesAsync();

            await _sut.AddAsync(Like("author", "l1", "p1", "alice"));

            (await _sut.GetUnreadCountAsync("author")).Should().Be(0);
            var (items, _) = await _sut.GetInboxAsync("author");
            items.Should().BeEmpty();

            (await _sut.ReleaseDeferredAsync("author")).Should().Be(1);
            (await _sut.GetUnreadCountAsync("author")).Should().Be(1);
        }

        [Fact]
        public async Task Inbox_OrdersUnreadThenPriorityThenNewest()
        {
            _context.Users.AddRange(MakeUser("author"), MakeUser("l1", "alice"), MakeUser("c1", "carol"));
            await _context.SaveChangesAsync();

            await _sut.AddAsync(Like("author", "l1", "p1", "alice"));
            await _sut.AddAsync(new Notification
            {
                Id = Guid.NewGuid().ToString(),
                RecipientId = "author",
                UserId = "c1",
                Type = NotificationActionTypes.Comment,
                Message = "carol commented.",
                PostId = "p1",
                LastActorName = "carol",
                CreatedAt = DateTime.UtcNow
            });

            var (items, total) = await _sut.GetInboxAsync("author");
            total.Should().Be(2);
            items.First().Type.Should().Be(NotificationActionTypes.Comment);

            // Read items sink below unread ones regardless of priority.
            await _sut.MarkAllAsReadAsync("author");
            await _sut.AddAsync(Like("author", "l1", "p2", "alice"));
            var (items2, _) = await _sut.GetInboxAsync("author");
            items2.First().PostId.Should().Be("p2");
        }

        [Fact]
        public async Task SelfAction_IsSkipped()
        {
            _context.Users.Add(MakeUser("author"));
            await _context.SaveChangesAsync();

            await _sut.AddAsync(Like("author", "author", "p1", "author"));

            (await _sut.GetByUserIdAsync("author")).Should().BeEmpty();
        }

        [Fact]
        public async Task DigestMode_CollapsesLowPriorityIntoDailyRow()
        {
            _context.Users.AddRange(MakeUser("author"), MakeUser("l1", "alice"));
            _context.NotificationPreferences.Add(new NotificationPreference { UserId = "author", DigestEnabled = true });
            await _context.SaveChangesAsync();

            await _sut.AddAsync(Like("author", "l1", "p1", "alice"));
            await _sut.AddAsync(Like("author", "l1", "p2", "alice"));

            var rows = await _sut.GetByUserIdAsync("author");
            rows.Should().ContainSingle();
            rows.Single().GroupKey.Should().StartWith("digest:like:");
        }

        [Theory]
        [InlineData(22, 7, 23, true)]
        [InlineData(22, 7, 6, true)]
        [InlineData(22, 7, 7, false)]
        [InlineData(22, 7, 12, false)]
        [InlineData(9, 17, 12, true)]
        [InlineData(9, 17, 8, false)]
        public void QuietWindow_EvaluatesCorrectly(int start, int end, int hour, bool expected)
        {
            var pref = new NotificationPreference { UserId = "u", QuietStartHourUtc = start, QuietEndHourUtc = end };
            pref.IsQuietNow(new DateTime(2026, 1, 1, hour, 30, 0, DateTimeKind.Utc)).Should().Be(expected);
        }

        [Fact]
        public async Task Preference_UpsertRoundtrip()
        {
            _context.Users.Add(MakeUser("u1"));
            await _context.SaveChangesAsync();

            var saved = await _sut.UpsertPreferenceAsync(new NotificationPreference
            {
                UserId = "u1",
                LikeEnabled = false,
                DigestEnabled = true,
                QuietStartHourUtc = 22,
                QuietEndHourUtc = 6
            });

            saved.LikeEnabled.Should().BeFalse();
            (await _sut.GetPreferenceAsync("u1")).Should().NotBeNull();

            saved.LikeEnabled = true;
            await _sut.UpsertPreferenceAsync(saved);
            (await _sut.GetPreferenceAsync("u1"))!.LikeEnabled.Should().BeTrue();
        }
    }
}
