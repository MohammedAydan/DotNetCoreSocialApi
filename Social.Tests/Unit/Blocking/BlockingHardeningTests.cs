using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Social.Application.Features.Admin.Users.Commands;
using Social.Application.Features.Users.Commands;
using Social.Core.Entities;
using Social.Core.Interfaces;
using Social.Infrastructure.Data;
using Social.Infrastructure.Repositories;
using Xunit;

namespace Social.Tests.Unit.Blocking
{
    public class BlockingHardeningTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ApplicationDbContext _context;

        public BlockingHardeningTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;
            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Close();
            _connection.Dispose();
        }

        private static User MakeUser(string id) => new()
        {
            Id = id,
            UserName = id,
            NormalizedUserName = id.ToUpperInvariant(),
            Email = $"{id}@test.local",
            NormalizedEmail = $"{id}@TEST.LOCAL".ToUpperInvariant(),
            FirstName = "Test",
            LastName = "User",
            PostsCount = 0,
        };

        private NotificationRepository NotificationRepo() => new(_context);
        private FollowRepository FollowRepo() => new(_context, NotificationRepo());
        private LikeRepository LikeRepo() => new(_context, NotificationRepo());
        private CommentRepository CommentRepo() => new(_context, NotificationRepo());

        [Fact]
        public async Task Follow_BlockedEitherDirection_Throws()
        {
            _context.Users.AddRange(MakeUser("a"), MakeUser("b"));
            _context.BlockUsers.Add(BlockUser.Create("a", "b"));
            await _context.SaveChangesAsync();

            var sut = FollowRepo();
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.FollowUserAsync("a", "b"));
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.FollowUserAsync("b", "a"));
        }

        [Fact]
        public async Task Follow_ListsExcludeBlocked()
        {
            _context.Users.AddRange(MakeUser("a"), MakeUser("b"), MakeUser("c"));
            _context.Followers.Add(new Follower { Id = "f1", FollowerId = "b", FollowingId = "a", Accepted = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            _context.Followers.Add(new Follower { Id = "f2", FollowerId = "c", FollowingId = "a", Accepted = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            _context.BlockUsers.Add(BlockUser.Create("a", "b"));
            await _context.SaveChangesAsync();

            var followers = await FollowRepo().GetFollowersAsync("a");
            followers.Select(f => f.FollowerId).Should().NotContain("b");
            followers.Select(f => f.FollowerId).Should().Contain("c");
        }

        [Fact]
        public async Task Like_BlockedLiker_Throws()
        {
            _context.Users.AddRange(MakeUser("author"), MakeUser("liker"));
            _context.Posts.Add(new Post { Id = "p1", UserId = "author", Content = "hi", Visibility = "public", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            _context.BlockUsers.Add(BlockUser.Create("author", "liker"));
            await _context.SaveChangesAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => LikeRepo().AddOrRemoveLikeAsync("p1", "liker"));
        }

        [Fact]
        public async Task Comment_BlockedCommenter_Throws()
        {
            _context.Users.AddRange(MakeUser("author"), MakeUser("commenter"));
            _context.Posts.Add(new Post { Id = "p1", UserId = "author", Content = "hi", Visibility = "public", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            _context.BlockUsers.Add(BlockUser.Create("author", "commenter"));
            await _context.SaveChangesAsync();

            var comment = new Comment { PostId = "p1", UserId = "commenter", Content = "hello" };
            await Assert.ThrowsAsync<InvalidOperationException>(() => CommentRepo().AddCommentAsync(comment));
        }

        [Fact]
        public async Task Notification_AcrossBlock_IsSuppressed()
        {
            _context.Users.AddRange(MakeUser("a"), MakeUser("b"));
            _context.BlockUsers.Add(BlockUser.Create("a", "b"));
            await _context.SaveChangesAsync();

            var repo = NotificationRepo();
            await repo.AddAsync(new Notification { Id = "n1", UserId = "a", RecipientId = "b", Type = "Follow", Message = "hi" });

            (await repo.GetByUserIdAsync("b")).Should().BeEmpty();
        }

        [Fact]
        public async Task Ban_EmptyReason_Throws()
        {
            var adminRepo = Substitute.For<IAdminRepository>();
            var auditRepo = Substitute.For<IAuditLogRepository>();
            var cache = Substitute.For<ICacheService>();
            var handler = new BanUserCommandHandler(adminRepo, auditRepo, cache);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                handler.Handle(new BanUserCommand("admin1", "a@x.com", "target1", "", 7), CancellationToken.None));
        }

        [Fact]
        public async Task Ban_NonPositiveDuration_Throws()
        {
            var adminRepo = Substitute.For<IAdminRepository>();
            var auditRepo = Substitute.For<IAuditLogRepository>();
            var cache = Substitute.For<ICacheService>();
            var handler = new BanUserCommandHandler(adminRepo, auditRepo, cache);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                handler.Handle(new BanUserCommand("admin1", "a@x.com", "target1", "spam", 0), CancellationToken.None));
        }

        [Fact]
        public async Task Ban_AdminTarget_Throws()
        {
            var adminRepo = Substitute.For<IAdminRepository>();
            var auditRepo = Substitute.For<IAuditLogRepository>();
            var cache = Substitute.For<ICacheService>();
            adminRepo.GetUserByIdAsync("target1", Arg.Any<CancellationToken>()).Returns(MakeUser("target1"));
            adminRepo.GetUserRolesAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(new List<string> { "Admin", "User" });
            var handler = new BanUserCommandHandler(adminRepo, auditRepo, cache);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new BanUserCommand("admin1", "a@x.com", "target1", "spam", 7), CancellationToken.None));
        }

        [Fact]
        public async Task Unban_NotLocked_Throws()
        {
            var adminRepo = Substitute.For<IAdminRepository>();
            var auditRepo = Substitute.For<IAuditLogRepository>();
            var cache = Substitute.For<ICacheService>();
            var unlocked = MakeUser("u1");
            unlocked.LockoutEnd = null;
            unlocked.LockoutEnabled = false;
            adminRepo.GetUserByIdAsync("u1", Arg.Any<CancellationToken>()).Returns(unlocked);
            var handler = new UnbanUserCommandHandler(adminRepo, auditRepo, cache);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new UnbanUserCommand("admin1", "a@x.com", "u1", "appeal"), CancellationToken.None));
        }

        [Fact]
        public async Task Refresh_LockedUser_Rejected()
        {
            var userRepo = Substitute.For<IUserRepository>();
            var tokenService = Substitute.For<ITokenService>();
            var mapper = Substitute.For<AutoMapper.IMapper>();
            var locked = MakeUser("banned1");
            locked.LockoutEnd = DateTimeOffset.UtcNow.AddDays(7);
            userRepo.ValidateRefreshTokenAsync("rt").Returns(new RefreshToken { Token = "rt", UserId = "banned1", Expires = DateTime.UtcNow.AddDays(1) });
            userRepo.GetUserByIdAsync("banned1", null, Arg.Any<CancellationToken>()).Returns(locked);
            var handler = new RefreshTokenCommandHandler(userRepo, tokenService, mapper);

            var result = await handler.Handle(new RefreshTokenCommand("rt"), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
        }
    }
}
