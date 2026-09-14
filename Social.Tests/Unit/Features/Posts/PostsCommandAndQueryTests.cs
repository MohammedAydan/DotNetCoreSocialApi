using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Social.Application.Features.Posts.Commands;
using Social.Application.Features.Posts.DTOs;
using Social.Application.Features.Posts.Queries;
using Social.Core.Entities;
using Social.Core.Interfaces;
using Xunit;

namespace Social.Tests.Unit.Features.Posts
{
    public class PostsCommandAndQueryTests
    {
        private readonly IPostRepository _postRepository = Substitute.For<IPostRepository>();
        private readonly IMapper _mapper = Substitute.For<IMapper>();

        [Fact]
        public async Task AddPost_WithValidData_ShouldReturnCreatedPostDto()
        {
            // Arrange
            var request = new CreatePostRequest { Content = "Hello world", Visibility = "public" };
            var post = new Post { Id = "p1", Content = "Hello world", UserId = "u1" };
            var postDto = new PostDto { Id = "p1", Content = "Hello world", UserId = "u1" };

            _mapper.Map<Post>(request).Returns(post);
            _postRepository.AddPostAsync(post).Returns(post);
            _mapper.Map<PostDto>(post).Returns(postDto);

            var handler = new AddPostCommandHandler(_postRepository, _mapper);

            // Act
            var result = await handler.Handle(new AddPostCommand(request, "u1"), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be("p1");
            result.Content.Should().Be("Hello world");
            await _postRepository.Received(1).AddPostAsync(Arg.Is<Post>(p => p.UserId == "u1"));
        }

        [Fact]
        public async Task SharePost_WithParentPostId_ShouldReturnSharedPostDto()
        {
            // Arrange
            var shareRequest = new SharePostRequest { ParentPostId = "parent-123", Content = "Check this out" };
            var post = new Post { Id = "share-1", Content = "Check this out", UserId = "u1", ParentPostId = "parent-123" };
            var postDto = new PostDto { Id = "share-1", Content = "Check this out", UserId = "u1" };

            _mapper.Map<Post>(shareRequest).Returns(post);
            _postRepository.SharePostAsync("parent-123", post).Returns(post);
            _mapper.Map<PostDto>(post).Returns(postDto);

            var handler = new SharePostCommandHandler(_postRepository, _mapper);

            // Act
            var result = await handler.Handle(new SharePostCommand(shareRequest, "u1"), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be("share-1");
            await _postRepository.Received(1).SharePostAsync("parent-123", Arg.Is<Post>(p => p.UserId == "u1"));
        }

        [Fact]
        public async Task DeletePost_WhenUserIsAuthorized_ShouldReturnTrue()
        {
            // Arrange
            _postRepository.DeletePostAsync("p1", "u1").Returns(true);
            var handler = new DeletePostCommandHandler(_postRepository);

            // Act
            var result = await handler.Handle(new DeletePostCommand("p1", "u1"), CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            await _postRepository.Received(1).DeletePostAsync("p1", "u1");
        }

        [Fact]
        public async Task GetPostById_WithValidId_ShouldReturnPostDto()
        {
            // Arrange
            var post = new Post { Id = "p1", Title = "Post 1" };
            var postDto = new PostDto { Id = "p1", Title = "Post 1" };

            _postRepository.GetPostByIdAsync("p1", "viewer-1").Returns(post);
            _mapper.Map<PostDto>(post).Returns(postDto);

            var handler = new GetPostByIdQueryHandler(_postRepository, _mapper);

            // Act
            var result = await handler.Handle(new GetPostByIdQuery("p1", "viewer-1"), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be("p1");
        }

        [Fact]
        public async Task GetFeedPosts_ShouldReturnPaginatedPosts()
        {
            // Arrange
            var posts = new List<Post>
            {
                new() { Id = "p1", Title = "Post 1" },
                new() { Id = "p2", Title = "Post 2" }
            };
            var postDtos = new List<PostDto>
            {
                new() { Id = "p1", Title = "Post 1" },
                new() { Id = "p2", Title = "Post 2" }
            };

            _postRepository.GetFeedPostsAsync("u1", 1, 10).Returns(posts);
            _mapper.Map<IEnumerable<PostDto>>(posts).Returns(postDtos);

            var handler = new GetFeedPostsQueryHandler(_postRepository, _mapper);

            // Act
            var result = await handler.Handle(new GetFeedPostsQuery("u1", 1, 10), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
        }
    }
}
