using FluentValidation;
using Social.Application.Features.Posts.Commands;

namespace Social.Application.Features.Posts.Validators
{
    public class AddPostCommandValidator : AbstractValidator<AddPostCommand>
    {
        public AddPostCommandValidator()
        {
            RuleFor(x => x.userId)
                .NotEmpty()
                .WithMessage("User ID is required.");

            RuleFor(x => x.createPost)
                .NotNull()
                .WithMessage("Post request body cannot be null.");

            When(x => x.createPost != null, () =>
            {
                RuleFor(x => x.createPost.Content)
                    .NotEmpty()
                    .When(x => x.createPost.Media == null || !x.createPost.Media.Any())
                    .WithMessage("Post content or media is required.");
            });
        }
    }
}
