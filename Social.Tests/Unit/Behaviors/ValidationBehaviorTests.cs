using FluentAssertions;
using FluentValidation;
using MediatR;
using NSubstitute;
using Social.Application.Behaviors;
using Social.Application.Features.Posts.Commands;
using Social.Application.Features.Posts.DTOs;
using Social.Application.Features.Posts.Validators;
using Xunit;

namespace Social.Tests.Unit.Behaviors
{
    public class ValidationBehaviorTests
    {
        [Fact]
        public async Task ValidationBehavior_WhenValidationFails_ShouldThrowValidationException()
        {
            // Arrange
            var validator = new AddPostCommandValidator();
            var validators = new List<IValidator<AddPostCommand>> { validator };
            var behavior = new ValidationBehavior<AddPostCommand, PostDto>(validators);

            var invalidCommand = new AddPostCommand(new CreatePostRequest { Content = "" }, ""); // Missing userId and content

            // Act
            Func<Task> act = async () => await behavior.Handle(invalidCommand, (cancellationToken) => Task.FromResult(new PostDto()), CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ValidationException>()
                .Where(ex => ex.Errors.Any(e => e.PropertyName == "userId"));
        }

        [Fact]
        public async Task ValidationBehavior_WhenValid_ShouldCallNext()
        {
            // Arrange
            var validator = new AddPostCommandValidator();
            var validators = new List<IValidator<AddPostCommand>> { validator };
            var behavior = new ValidationBehavior<AddPostCommand, PostDto>(validators);

            var validCommand = new AddPostCommand(new CreatePostRequest { Content = "Valid content" }, "u1");
            var expectedDto = new PostDto { Id = "p1", Content = "Valid content" };

            // Act
            var result = await behavior.Handle(validCommand, (cancellationToken) => Task.FromResult(expectedDto), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Content.Should().Be("Valid content");
        }

        [Fact]
        public async Task ValidationBehavior_WhenNoValidators_ShouldCallNext()
        {
            // Arrange
            var validators = Enumerable.Empty<IValidator<AddPostCommand>>();
            var behavior = new ValidationBehavior<AddPostCommand, PostDto>(validators);

            var command = new AddPostCommand(new CreatePostRequest(), "u1");
            var expectedDto = new PostDto { Id = "p1" };

            // Act
            var result = await behavior.Handle(command, (cancellationToken) => Task.FromResult(expectedDto), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
        }
    }
}
