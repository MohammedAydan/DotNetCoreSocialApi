using FluentAssertions;
using NSubstitute;
using Social.Application.Features.Posts.DTOs;
using Social.Core.Common;
using Social.Core.Entities;
using Social.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Social.Tests.Integration.Controllers
{
    public class PostsControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public PostsControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task AddPost_WhenAuthenticated_ShouldReturnOkWithPostDto()
        {
            // Arrange
            var request = new CreatePostRequest { Content = "Hello from integration test!" };
            var post = new Post
            {
                Id = "post-100",
                UserId = TestAuthHandler.DefaultUserId,
                Content = "Hello from integration test!"
            };

            _factory.MockPostRepository.AddPostAsync(Arg.Any<Post>()).Returns(post);

            // Act
            var response = await _client.PostAsJsonAsync("/api/posts", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<PostDto>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
            content.Data!.Id.Should().Be("post-100");
            content.Data.Content.Should().Be("Hello from integration test!");
        }

        [Fact]
        public async Task AddPost_WhenAnonymous_ShouldReturnUnauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Anonymous", "true");
            var request = new CreatePostRequest { Content = "Unauthorized post" };

            // Act
            var response = await client.PostAsJsonAsync("/api/posts", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetPostById_WhenExists_ShouldReturnPostDto()
        {
            // Arrange
            var post = new Post
            {
                Id = "post-200",
                UserId = TestAuthHandler.DefaultUserId,
                Content = "Integration test post content"
            };

            _factory.MockPostRepository.GetPostByIdAsync("post-200", TestAuthHandler.DefaultUserId).Returns(post);

            // Act
            var response = await _client.GetAsync("/api/posts/post-200");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<PostDto>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
            content.Data!.Id.Should().Be("post-200");
        }

        [Fact]
        public async Task GetFeed_WhenAuthenticated_ShouldReturnPostList()
        {
            // Arrange
            var posts = new List<Post>
            {
                new() { Id = "feed-1", UserId = "u1", Content = "Feed post 1" },
                new() { Id = "feed-2", UserId = "u2", Content = "Feed post 2" }
            };

            _factory.MockPostRepository.GetFeedPostsAsync(TestAuthHandler.DefaultUserId, 1, 20).Returns(posts);

            // Act
            var response = await _client.GetAsync("/api/posts/feed?Page=1&Limit=20");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<PostDto>>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
            content.Data.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetMyPosts_WhenAuthenticated_ShouldReturnUserPosts()
        {
            // Arrange
            var posts = new List<Post>
            {
                new() { Id = "my-1", UserId = TestAuthHandler.DefaultUserId, Content = "My post 1" }
            };

            _factory.MockPostRepository.GetMyPostsAsync(TestAuthHandler.DefaultUserId, 1, 20)
                .Returns(posts);

            // Act
            var response = await _client.GetAsync("/api/posts/my-posts?Page=1&Limit=20");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<PostDto>>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
            content.Data.Should().HaveCount(1);
        }

        [Fact]
        public async Task DeletePost_WhenPostExists_ShouldReturnOk()
        {
            // Arrange
            _factory.MockPostRepository.DeletePostAsync("post-to-delete", TestAuthHandler.DefaultUserId).Returns(true);

            // Act
            var response = await _client.DeleteAsync("/api/posts/post-to-delete");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
        }
    }
}
