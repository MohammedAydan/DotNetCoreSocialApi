using AutoMapper;
using FluentAssertions;
using Moq;
using Social.Application.Features.Comments.Commands;
using Social.Application.Features.Comments.DTOs;
using Social.Core.Entities;
using Social.Core.Interfaces;
using Xunit;

namespace Social.Tests.Application.Features.Comments.Commands
{
    public class UpdateCommentCommandTests
    {
        private readonly Mock<ICommentRepository> _mockCommentRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly UpdateCommentCommandHandler _handler;

        public UpdateCommentCommandTests()
        {
            _mockCommentRepository = new Mock<ICommentRepository>();
            _mockMapper = new Mock<IMapper>();
            _handler = new UpdateCommentCommandHandler(_mockCommentRepository.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task Handle_WithValidData_ShouldReturnUpdatedComment()
        {
            // Arrange
            var userId = "user123";
            var commentId = "comment123";
            var updateRequest = new UpdateCommentRequest
            {
                Id = commentId,
                Content = "Updated comment content"
            };
            var command = new UpdateCommentCommand(updateRequest, userId);

            var mappedComment = new Comment
            {
                Id = commentId,
                Content = updateRequest.Content,
                UserId = userId
            };

            var updatedComment = new Comment
            {
                Id = commentId,
                Content = updateRequest.Content,
                UserId = userId,
                UpdatedAt = DateTime.UtcNow
            };

            var expectedDto = new CommentDto
            {
                Id = commentId,
                Content = updateRequest.Content,
                UserId = userId,
                FirstName = "Test",
                LastName = "User",
                PostId = "post123",
                ParentId = "",
                RepliesCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockMapper.Setup(m => m.Map<Comment>(updateRequest))
                .Returns(mappedComment);

            _mockCommentRepository.Setup(r => r.UpdateCommentAsync(mappedComment, userId))
                .ReturnsAsync(updatedComment);

            _mockMapper.Setup(m => m.Map<CommentDto>(updatedComment))
                .Returns(expectedDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(commentId);
            result.Content.Should().Be(updateRequest.Content);
            result.UserId.Should().Be(userId);

            _mockCommentRepository.Verify(r => r.UpdateCommentAsync(mappedComment, userId), Times.Once);
            _mockMapper.Verify(m => m.Map<Comment>(updateRequest), Times.Once);
            _mockMapper.Verify(m => m.Map<CommentDto>(updatedComment), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenRepositoryReturnsNull_ShouldThrowException()
        {
            // Arrange
            var userId = "user123";
            var updateRequest = new UpdateCommentRequest
            {
                Id = "comment123",
                Content = "Updated content"
            };
            var command = new UpdateCommentCommand(updateRequest, userId);

            var mappedComment = new Comment
            {
                Id = updateRequest.Id,
                Content = updateRequest.Content
            };

            _mockMapper.Setup(m => m.Map<Comment>(updateRequest))
                .Returns(mappedComment);

            _mockCommentRepository.Setup(r => r.UpdateCommentAsync(It.IsAny<Comment>(), userId))
                .ReturnsAsync((Comment?)null);

            // Act & Assert
            var action = async () => await _handler.Handle(command, CancellationToken.None);

            await action.Should().ThrowAsync<Exception>()
                .WithMessage("Failed to update comment.");
        }
    }
}