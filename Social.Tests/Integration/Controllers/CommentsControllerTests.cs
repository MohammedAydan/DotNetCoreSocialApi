using FluentAssertions;
using NSubstitute;
using Social.Application.Features.Comments.DTOs;
using Social.Core.Common;
using Social.Core.Entities;
using Social.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Social.Tests.Integration.Controllers
{
    public class CommentsControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CommentsControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CreateComment_WithValidData_ShouldReturnOkWithCommentDto()
        {
            // Arrange
            var request = new CreateCommentRequest
            {
                PostId = "post-1",
                Content = "Great post!"
            };

            var comment = new Comment
            {
                Id = "c-100",
                PostId = "post-1",
                UserId = TestAuthHandler.DefaultUserId,
                Content = "Great post!"
            };

            _factory.MockCommentRepository.AddCommentAsync(Arg.Any<Comment>()).Returns(comment);

            // Act
            var response = await _client.PostAsJsonAsync("/api/comments", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<CommentDto>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
            content.Data!.Id.Should().Be("c-100");
            content.Data.Content.Should().Be("Great post!");
        }

        [Fact]
        public async Task CreateComment_WithMissingContent_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new CreateCommentRequest
            {
                PostId = "post-1",
                Content = ""
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/comments", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetCommentsByPostId_ShouldReturnCommentList()
        {
            // Arrange
            var comments = new List<Comment>
            {
                new() { Id = "c-1", PostId = "post-1", UserId = "u1", Content = "Comment 1" },
                new() { Id = "c-2", PostId = "post-1", UserId = "u2", Content = "Comment 2" }
            };

            _factory.MockCommentRepository.GetCommentsByPostIdAsync("post-1", 1, 10).Returns(comments);

            // Act
            var response = await _client.GetAsync("/api/comments/post/post-1?page=1&limit=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<CommentDto>>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
            content.Data.Should().HaveCount(2);
        }

        [Fact]
        public async Task UpdateComment_WhenValid_ShouldReturnUpdatedComment()
        {
            // Arrange
            var request = new UpdateCommentRequest
            {
                Id = "c-1",
                Content = "Updated comment content"
            };

            var existingComment = new Comment
            {
                Id = "c-1",
                UserId = TestAuthHandler.DefaultUserId,
                Content = "Original comment"
            };

            var updatedComment = new Comment
            {
                Id = "c-1",
                UserId = TestAuthHandler.DefaultUserId,
                Content = "Updated comment content"
            };

            _factory.MockCommentRepository.GetCommentByIdAsync("c-1").Returns(existingComment);
            _factory.MockCommentRepository.UpdateCommentAsync(Arg.Any<Comment>(), TestAuthHandler.DefaultUserId).Returns(updatedComment);

            // Act
            var response = await _client.PutAsJsonAsync("/api/comments/c-1", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<CommentDto>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
            content.Data!.Content.Should().Be("Updated comment content");
        }

        [Fact]
        public async Task DeleteComment_WhenAuthorized_ShouldReturnOk()
        {
            // Arrange
            var existingComment = new Comment
            {
                Id = "c-1",
                UserId = TestAuthHandler.DefaultUserId,
                Content = "Comment to delete"
            };

            _factory.MockCommentRepository.GetCommentByIdAsync("c-1").Returns(existingComment);
            _factory.MockCommentRepository.DeleteCommentAsync("c-1", TestAuthHandler.DefaultUserId).Returns(true);

            // Act
            var response = await _client.DeleteAsync("/api/comments/c-1");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
        }
    }
}
