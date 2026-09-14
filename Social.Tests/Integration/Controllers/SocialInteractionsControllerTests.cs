using FluentAssertions;
using NSubstitute;
using Social.Application.Features.BlockUser.DTOs;
using Social.Application.Features.BlockUser.Requests;
using Social.Application.Features.Follow.DTOs;
using Social.Application.Features.Followers.DTOs;
using Social.Application.Features.Like.DTOs;
using Social.Application.Features.Notifications.DTOs;
using Social.Core.Common;
using Social.Core.Entities;
using Social.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Social.Tests.Integration.Controllers
{
    public class SocialInteractionsControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public SocialInteractionsControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        // ================= Follow Tests =================
        [Fact]
        public async Task Follow_WithValidData_ShouldReturnOkWithFollowerDto()
        {
            // Arrange
            var request = new FollowRequest
            {
                FollowerId = TestAuthHandler.DefaultUserId,
                TargetUserId = "target-user-1"
            };

            var follower = new Follower
            {
                Id = "f-100",
                FollowerId = TestAuthHandler.DefaultUserId,
                FollowingId = "target-user-1",
                Accepted = false
            };

            _factory.MockFollowRepository.FollowUserAsync(TestAuthHandler.DefaultUserId, "target-user-1").Returns(follower);

            // Act
            var response = await _client.PostAsJsonAsync("/api/Follow/follow", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<FollowerDto>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
            content.Data!.Id.Should().Be("f-100");
        }

        [Fact]
        public async Task Unfollow_WithValidData_ShouldReturnOk()
        {
            // Arrange
            var request = new FollowRequest
            {
                FollowerId = TestAuthHandler.DefaultUserId,
                TargetUserId = "target-user-1"
            };

            _factory.MockFollowRepository.UnfollowUserAsync(TestAuthHandler.DefaultUserId, "target-user-1").Returns(true);

            // Act
            var response = await _client.PostAsJsonAsync("/api/Follow/unfollow", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
            content.Data.Should().BeTrue();
        }

        [Fact]
        public async Task GetFollowers_WhenAuthenticated_ShouldReturnList()
        {
            // Arrange
            var list = new List<Follower>
            {
                new() { Id = "f-1", FollowerId = "follower-1", FollowingId = TestAuthHandler.DefaultUserId }
            };

            _factory.MockFollowRepository.GetFollowersAsync(TestAuthHandler.DefaultUserId, 1, 20).Returns(list);

            // Act
            var response = await _client.GetAsync("/api/Follow/followers?page=1&limit=20");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<FollowerDto>>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
            content.Data.Should().HaveCount(1);
        }

        // ================= Like Tests =================
        [Fact]
        public async Task AddOrRemoveLike_WhenAuthenticated_ShouldReturnOk()
        {
            // Arrange
            var request = new LikeRequest { PostId = "post-like-1" };
            _factory.MockLikeRepository.AddOrRemoveLikeAsync("post-like-1", TestAuthHandler.DefaultUserId).Returns(true);

            // Act
            var response = await _client.PostAsJsonAsync("/api/Like", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
            content.Data.Should().BeTrue();
        }

        [Fact]
        public async Task GetLikesByPostId_ShouldReturnLikesList()
        {
            // Arrange
            var likes = new List<Like>
            {
                new() { Id = "l-1", PostId = "post-like-1", UserId = "u1" }
            };

            _factory.MockLikeRepository.GetLikesByPostIdAsync("post-like-1").Returns(likes);

            // Act
            var response = await _client.GetAsync("/api/Like/post-like-1");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<LikeDto>>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
            content.Data.Should().HaveCount(1);
        }

        // ================= BlockUser Tests =================
        [Fact]
        public async Task BlockUser_WhenValid_ShouldReturnOk()
        {
            // Arrange
            var request = new BlockUserRequest { BlockedUserId = "target-block-1" };
            _factory.MockUserRepository.GetUserByIdAsync("target-block-1").Returns(new User { Id = "target-block-1" });
            _factory.MockBlockUserRepository.IsUserBlockedAsync(TestAuthHandler.DefaultUserId, "target-block-1").Returns(false);
            _factory.MockBlockUserRepository.BlockUserAsync(TestAuthHandler.DefaultUserId, "target-block-1").Returns(Task.CompletedTask);

            // Act
            var response = await _client.PostAsJsonAsync("/api/BlockUser/block", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
        }

        [Fact]
        public async Task GetBlockedUsers_WhenAuthenticated_ShouldReturnList()
        {
            // Arrange
            var blockedUsers = new List<BlockUser>
            {
                new() { Id = "b-1", UserId = TestAuthHandler.DefaultUserId, BlockedUserId = "blocked-user-1" }
            };

            _factory.MockBlockUserRepository.GetBlockedUsersAsync(TestAuthHandler.DefaultUserId, 1, 20).Returns(blockedUsers);

            // Act
            var response = await _client.GetAsync("/api/BlockUser/blocked-users?page=1&limit=20");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<BlockUserDto>>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
            content.Data.Should().HaveCount(1);
        }

        // ================= Notifications Tests =================
        [Fact]
        public async Task CreateNotification_WithValidData_ShouldReturnOkWithNotificationDto()
        {
            // Arrange
            var request = new CreateNotificationDto
            {
                UserId = TestAuthHandler.DefaultUserId,
                Type = "like",
                Message = "New notification received"
            };

            _factory.MockNotificationRepository.AddAsync(Arg.Any<Notification>()).Returns(Task.CompletedTask);

            // Act
            var response = await _client.PostAsJsonAsync("/api/Notifications", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<NotificationDto>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
            content.Data!.UserId.Should().Be(TestAuthHandler.DefaultUserId);
        }

        [Fact]
        public async Task GetNotificationById_WhenExists_ShouldReturnNotification()
        {
            // Arrange
            var notification = new Notification
            {
                Id = "notif-1",
                UserId = TestAuthHandler.DefaultUserId,
                Message = "Integration test notification"
            };

            _factory.MockNotificationRepository.GetByIdAsync("notif-1").Returns(notification);

            // Act
            var response = await _client.GetAsync("/api/Notifications/notif-1");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<NotificationDto>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
            content.Data!.Id.Should().Be("notif-1");
        }

        [Fact]
        public async Task MarkAsRead_WhenAuthorized_ShouldReturnOk()
        {
            // Arrange
            var notification = new Notification
            {
                Id = TestAuthHandler.DefaultUserId, // NotificationsController.MarkAsRead checks IsAuthorizedUser(id)
                UserId = TestAuthHandler.DefaultUserId,
                IsRead = false
            };

            _factory.MockNotificationRepository.GetByIdAsync(TestAuthHandler.DefaultUserId).Returns(notification);
            _factory.MockNotificationRepository.UpdateAsync(notification).Returns(Task.CompletedTask);

            // Act
            var response = await _client.PostAsync($"/api/Notifications/{TestAuthHandler.DefaultUserId}/mark-read", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
        }
    }
}
