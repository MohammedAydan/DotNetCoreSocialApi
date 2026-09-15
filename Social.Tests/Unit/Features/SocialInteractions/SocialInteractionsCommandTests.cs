using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Social.Application.Features.BlockUser.Commands;
using Social.Application.Features.BlockUser.DTOs;
using Social.Application.Features.BlockUser.Queries;
using Social.Application.Features.BlockUser.Requests;
using Social.Application.Features.Follow.Commands;
using Social.Application.Features.Follow.DTOs;
using Social.Application.Features.Follow.Queries;
using Social.Application.Features.Followers.DTOs;
using Social.Application.Features.Like.Commands;
using Social.Application.Features.Like.DTOs;
using Social.Application.Features.Like.Queries;
using Social.Application.Features.Notifications.Commands;
using Social.Application.Features.Notifications.DTOs;
using Social.Core.Entities;
using Social.Core.Interfaces;
using Xunit;

namespace Social.Tests.Unit.Features.SocialInteractions
{
    public class SocialInteractionsCommandTests
    {
        private readonly IFollowRepository _followRepository = Substitute.For<IFollowRepository>();
        private readonly ILikeRepository _likeRepository = Substitute.For<ILikeRepository>();
        private readonly IBlockUserRepository _blockUserRepository = Substitute.For<IBlockUserRepository>();
        private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
        private readonly INotificationRepository _notificationRepository = Substitute.For<INotificationRepository>();
        private readonly IMapper _mapper = Substitute.For<IMapper>();

        // Follow Tests
        [Fact]
        public async Task FollowUser_WhenSuccessful_ShouldReturnFollowerDto()
        {
            // Arrange
            var request = new FollowRequest { FollowerId = "u1", TargetUserId = "u2" };
            var follower = new Follower { Id = "f1", FollowerId = "u1", FollowingId = "u2" };
            var followerDto = new FollowerDto { Id = "f1", FollowerId = "u1", FollowingId = "u2" };

            _followRepository.FollowUserAsync("u1", "u2").Returns(follower);
            _mapper.Map<FollowerDto>(follower).Returns(followerDto);

            var handler = new FollowUserCommandHandler(_followRepository, _mapper);

            // Act
            var result = await handler.Handle(new FollowUserCommand(request), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be("f1");
            await _followRepository.Received(1).FollowUserAsync("u1", "u2");
        }

        [Fact]
        public async Task UnfollowUser_ShouldReturnTrue()
        {
            // Arrange
            var request = new FollowRequest { FollowerId = "u1", TargetUserId = "u2" };
            _followRepository.UnfollowUserAsync("u1", "u2").Returns(true);

            var handler = new UnfollowUserCommandHandler(_followRepository);

            // Act
            var result = await handler.Handle(new UnfollowUserCommand(request), CancellationToken.None);

            // Assert
            result.Should().BeTrue();
        }

        // Like Tests
        [Fact]
        public async Task AddOrRemoveLike_ShouldCallRepositoryAndReturnBool()
        {
            // Arrange
            var request = new LikeRequest { PostId = "p1" };
            _likeRepository.AddOrRemoveLikeAsync("p1", "u1").Returns(true);

            var handler = new AddOrRemoveLikeCommandHandler(_likeRepository);

            // Act
            var result = await handler.Handle(new AddOrRemoveLikeCommand(request, "u1"), CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            await _likeRepository.Received(1).AddOrRemoveLikeAsync("p1", "u1");
        }

        [Fact]
        public async Task GetLikesByPostId_ShouldReturnLikeList()
        {
            // Arrange
            var likes = new List<Like>
            {
                new() { Id = "l1", PostId = "p1", UserId = "u1" },
                new() { Id = "l2", PostId = "p1", UserId = "u2" }
            };
            var likeDtos = new List<LikeDto>
            {
                new() { Id = "l1", PostId = "p1", UserId = "u1" },
                new() { Id = "l2", PostId = "p1", UserId = "u2" }
            };

            _likeRepository.GetLikesByPostIdAsync("p1").Returns(likes);
            _mapper.Map<IEnumerable<LikeDto>>(likes).Returns(likeDtos);

            var handler = new GetLikesByPostIdQueryHandler(_likeRepository, _mapper);

            // Act
            var result = await handler.Handle(new GetLikesByPostIdQuery("p1"), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
        }

        // BlockUser Tests
        [Fact]
        public async Task BlockUser_WhenValid_ShouldCallRepository()
        {
            // Arrange
            var request = new BlockUserRequest { BlockedUserId = "target-1" };
            _userRepository.GetUserByIdAsync("target-1").Returns(new User { Id = "target-1" });
            _blockUserRepository.IsUserBlockedAsync("blocker-1", "target-1").Returns(false);

            var handler = new BlockUserCommandHandler(_blockUserRepository, _userRepository);

            // Act
            await handler.Handle(new BlockUserCommand(request, "blocker-1"), CancellationToken.None);

            // Assert
            await _blockUserRepository.Received(1).BlockUserAsync("blocker-1", "target-1");
        }

        [Fact]
        public async Task BlockUser_WhenBlockingSelf_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var request = new BlockUserRequest { BlockedUserId = "same-user" };
            var handler = new BlockUserCommandHandler(_blockUserRepository, _userRepository);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(new BlockUserCommand(request, "same-user"), CancellationToken.None));
        }

        [Fact]
        public async Task UnblockUser_WhenBlocked_ShouldUnblock()
        {
            // Arrange
            var request = new BlockUserRequest { BlockedUserId = "target-1" };
            _userRepository.GetUserByIdAsync("target-1").Returns(new User { Id = "target-1" });
            _blockUserRepository.IsUserBlockedAsync("blocker-1", "target-1").Returns(true);

            var handler = new UnblockUserCommandHandler(_blockUserRepository, _userRepository);

            // Act
            await handler.Handle(new UnblockUserCommand(request, "blocker-1"), CancellationToken.None);

            // Assert
            await _blockUserRepository.Received(1).UnblockUserAsync("blocker-1", "target-1");
        }

        // Notification Tests
        [Fact]
        public async Task CreateNotification_WithValidData_ShouldSaveAndReturnDto()
        {
            // Arrange
            var createDto = new CreateNotificationDto
            {
                UserId = "u1",
                RecipientId = "u2",
                Type = "like",
                Message = "Someone liked your post"
            };

            _notificationRepository.AddAsync(Arg.Any<Notification>()).Returns(Task.CompletedTask);

            var handler = new CreateNotificationCommandHandler(_notificationRepository);

            // Act
            var result = await handler.Handle(new CreateNotificationCommand(createDto), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be("u1");
            result.Message.Should().Be("Someone liked your post");
            await _notificationRepository.Received(1).AddAsync(Arg.Is<Notification>(n => n.UserId == "u1"));
        }

        [Fact]
        public async Task MarkNotificationAsRead_ShouldCallRepository()
        {
            // Arrange
            var notification = new Notification { Id = "n1", UserId = "u1", IsRead = false };
            _notificationRepository.GetByIdAsync("n1").Returns(notification);
            _notificationRepository.UpdateAsync(notification).Returns(Task.CompletedTask);

            var handler = new MarkNotificationAsReadCommandHandler(_notificationRepository);

            // Act
            await handler.Handle(new MarkNotificationAsReadCommand("n1"), CancellationToken.None);

            // Assert
            await _notificationRepository.Received(1).UpdateAsync(Arg.Is<Notification>(n => n.IsRead == true));
        }

        [Fact]
        public async Task AcceptFollow_ShouldCallRepositoryAndReturnTrue()
        {
            // Arrange
            var request = new FollowRequest { FollowerId = "u1", TargetUserId = "u2" };
            _followRepository.AcceptFollowRequestAsync("u2", "u1").Returns(true);

            var handler = new AcceptFollowCommandHandler(_followRepository);

            // Act
            var result = await handler.Handle(new AcceptFollowCommand(request), CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            await _followRepository.Received(1).AcceptFollowRequestAsync("u2", "u1");
        }

        [Fact]
        public async Task RejectFollow_ShouldCallRepositoryAndReturnTrue()
        {
            // Arrange
            var request = new FollowRequest { FollowerId = "u1", TargetUserId = "u2" };
            _followRepository.RejectFollowRequestAsync("u2", "u1").Returns(true);

            var handler = new RejectFollowCommandHandler(_followRepository);

            // Act
            var result = await handler.Handle(new RejectFollowCommand(request), CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            await _followRepository.Received(1).RejectFollowRequestAsync("u2", "u1");
        }

        [Fact]
        public async Task GetFollowers_WhenFollowersExist_ShouldReturnDtos()
        {
            // Arrange
            var followers = new List<Follower> { new() { Id = "f1", FollowerId = "u1", FollowingId = "u2" } };
            var dtos = new List<FollowerDto> { new() { Id = "f1", FollowerId = "u1", FollowingId = "u2" } };

            _followRepository.GetFollowersAsync("u2", 1, 20).Returns(followers);
            _mapper.Map<IEnumerable<FollowerDto>>(followers).Returns(dtos);

            var handler = new GetFollowersQueryHandler(_followRepository, _mapper);

            // Act
            var result = await handler.Handle(new GetFollowersQuery("u2", 1, 20), CancellationToken.None);

            // Assert
            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetFollowing_WhenFollowingExist_ShouldReturnDtos()
        {
            // Arrange
            var followers = new List<Follower> { new() { Id = "f1", FollowerId = "u2", FollowingId = "u1" } };
            var dtos = new List<FollowerDto> { new() { Id = "f1", FollowerId = "u2", FollowingId = "u1" } };

            _followRepository.GetFollowingAsync("u2", 1, 20).Returns(followers);
            _mapper.Map<IEnumerable<FollowerDto>>(followers).Returns(dtos);

            var handler = new GetFollowingQueryHandler(_followRepository, _mapper);

            // Act
            var result = await handler.Handle(new GetFollowingQuery("u2", 1, 20), CancellationToken.None);

            // Assert
            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetPendingFollowRequest_WhenPendingExist_ShouldReturnDtos()
        {
            // Arrange
            var followers = new List<Follower> { new() { Id = "f1", FollowerId = "u1", FollowingId = "u2", Accepted = false } };
            var dtos = new List<FollowerDto> { new() { Id = "f1", FollowerId = "u1", FollowingId = "u2", Accepted = false } };

            _followRepository.GetPendingFollowRequestsAsync("u2", 1, 20).Returns(followers);
            _mapper.Map<IEnumerable<FollowerDto>>(followers).Returns(dtos);

            var handler = new GetPendingFollowRequestQueryHandler(_followRepository, _mapper);

            // Act
            var result = await handler.Handle(new GetPendingFollowRequestQuery("u2", 1, 20), CancellationToken.None);

            // Assert
            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetBlockedUsers_WhenBlockedUsersExist_ShouldReturnDtos()
        {
            // Arrange
            var blockedUsers = new List<BlockUser> { new() { Id = "b1", UserId = "u1", BlockedUserId = "u2" } };
            var dtos = new List<BlockUserDto> { new() { Id = "b1", UserId = "u1" } };

            _blockUserRepository.GetBlockedUsersAsync("u1", 1, 20).Returns(blockedUsers);
            _mapper.Map<IEnumerable<BlockUserDto>>(blockedUsers).Returns(dtos);

            var handler = new GetBlockedUsersQueryHandler(_blockUserRepository, _mapper);

            // Act
            var result = await handler.Handle(new GetBlockedUsersQuery("u1", 1, 20), CancellationToken.None);

            // Assert
            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task IsUserBlocked_ShouldReturnTrueIfBlocked()
        {
            // Arrange
            _blockUserRepository.IsUserBlockedAsync("u1", "u2").Returns(true);
            var handler = new IsUserBlockedQueryHandler(_blockUserRepository);

            // Act
            var result = await handler.Handle(new IsUserBlockedQuery("u1", "u2"), CancellationToken.None);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task MarkAllNotificationsAsRead_ShouldUpdateAllUnread()
        {
            // Arrange
            var list = new List<Notification>
            {
                new() { Id = "n1", UserId = "u1", IsRead = false },
                new() { Id = "n2", UserId = "u1", IsRead = false }
            };
            _notificationRepository.GetUnreadByUserIdAsync("u1").Returns(list);
            _notificationRepository.UpdateAsync(Arg.Any<Notification>()).Returns(Task.CompletedTask);

            var handler = new MarkAllNotificationsAsReadCommandHandler(_notificationRepository);

            // Act
            await handler.Handle(new MarkAllNotificationsAsReadCommand("u1"), CancellationToken.None);

            // Assert
            list.Should().OnlyContain(n => n.IsRead);
            await _notificationRepository.Received(2).UpdateAsync(Arg.Any<Notification>());
        }

        [Fact]
        public async Task DeleteNotification_WhenExists_ShouldDelete()
        {
            // Arrange
            var notification = new Notification { Id = "n1", UserId = "u1" };
            _notificationRepository.GetByIdAsync("n1").Returns(notification);
            _notificationRepository.DeleteAsync("n1").Returns(Task.CompletedTask);

            var handler = new DeleteNotificationCommandHandler(_notificationRepository);

            // Act
            await handler.Handle(new DeleteNotificationCommand("n1"), CancellationToken.None);

            // Assert
            await _notificationRepository.Received(1).DeleteAsync("n1");
        }

        [Fact]
        public async Task DeleteAllNotificationsForUser_ShouldDeleteAll()
        {
            // Arrange
            var list = new List<Notification>
            {
                new() { Id = "n1", UserId = "u1" },
                new() { Id = "n2", UserId = "u1" }
            };
            _notificationRepository.GetByUserIdAsync("u1").Returns(list);
            _notificationRepository.DeleteAsync(Arg.Any<string>()).Returns(Task.CompletedTask);

            var handler = new DeleteAllNotificationsForUserCommandHandler(_notificationRepository);

            // Act
            await handler.Handle(new DeleteAllNotificationsForUserCommand("u1"), CancellationToken.None);

            // Assert
            await _notificationRepository.Received(2).DeleteAsync(Arg.Any<string>());
        }
    }
}
