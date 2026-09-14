using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Social.Application.Features.Comments.Commands;
using Social.Application.Features.Comments.DTOs;
using Social.Application.Features.Comments.Queries;
using Social.Core.Entities;
using Social.Core.Interfaces;
using Xunit;

namespace Social.Tests.Unit.Features.Comments
{
    public class CommentsCommandAndQueryTests
    {
        private readonly ICommentRepository _commentRepository = Substitute.For<ICommentRepository>();
        private readonly IMapper _mapper = Substitute.For<IMapper>();

        [Fact]
        public async Task AddComment_WithValidRequest_ShouldReturnCommentDto()
        {
            // Arrange
            var request = new CreateCommentRequest { PostId = "p1", Content = "Great post!" };
            var comment = new Comment { Id = "c1", PostId = "p1", Content = "Great post!", UserId = "u1" };
            var commentDto = new CommentDto { Id = "c1", PostId = "p1", Content = "Great post!", UserId = "u1" };

            _mapper.Map<Comment>(request).Returns(comment);
            _commentRepository.AddCommentAsync(comment).Returns(comment);
            _mapper.Map<CommentDto>(comment).Returns(commentDto);

            var handler = new AddCommentCommandHandler(_commentRepository, _mapper);

            // Act
            var result = await handler.Handle(new AddCommentCommand(request, "u1"), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be("c1");
            result.Content.Should().Be("Great post!");
            await _commentRepository.Received(1).AddCommentAsync(Arg.Is<Comment>(c => c.UserId == "u1"));
        }

        [Fact]
        public async Task AddReplyComment_WithValidRequest_ShouldReturnReplyDto()
        {
            // Arrange
            var request = new CreateReplyCommentRequest { PostId = "p1", ParentId = "c1", Content = "Thanks!" };
            var reply = new Comment { Id = "reply-1", PostId = "p1", ParentId = "c1", Content = "Thanks!", UserId = "u2" };
            var replyDto = new CommentDto { Id = "reply-1", PostId = "p1", ParentId = "c1", Content = "Thanks!", UserId = "u2" };

            _mapper.Map<Comment>(request).Returns(reply);
            _commentRepository.AddReplyAsync(reply).Returns(reply);
            _mapper.Map<CommentDto>(reply).Returns(replyDto);

            var handler = new AddReplyCommentCommandHandler(_commentRepository, _mapper);

            // Act
            var result = await handler.Handle(new AddReplyCommentCommand(request, "u2"), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be("reply-1");
            await _commentRepository.Received(1).AddReplyAsync(Arg.Is<Comment>(c => c.UserId == "u2"));
        }

        [Fact]
        public async Task DeleteComment_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            _commentRepository.DeleteCommentAsync("c1", "u1").Returns(true);
            var handler = new DeleteCommentCommandHandler(_commentRepository);

            // Act
            var result = await handler.Handle(new DeleteCommentCommand("c1", "u1"), CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            await _commentRepository.Received(1).DeleteCommentAsync("c1", "u1");
        }

        [Fact]
        public async Task GetCommentsByPostId_ShouldReturnListOfComments()
        {
            // Arrange
            var comments = new List<Comment>
            {
                new() { Id = "c1", Content = "Comment 1" },
                new() { Id = "c2", Content = "Comment 2" }
            };
            var commentDtos = new List<CommentDto>
            {
                new() { Id = "c1", Content = "Comment 1" },
                new() { Id = "c2", Content = "Comment 2" }
            };

            _commentRepository.GetCommentsByPostIdAsync("p1", 1, 10).Returns(comments);
            _mapper.Map<IEnumerable<CommentDto>>(comments).Returns(commentDtos);

            var handler = new GetCommentsByPostIdQueryHandler(_commentRepository, _mapper);

            // Act
            var result = await handler.Handle(new GetCommentsByPostIdQuery("p1", 1, 10), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
        }
    }
}
