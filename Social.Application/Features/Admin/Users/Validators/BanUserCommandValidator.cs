using FluentValidation;
using Social.Application.Features.Admin.Users.Commands;

namespace Social.Application.Features.Admin.Users.Validators
{
    public class BanUserCommandValidator : AbstractValidator<BanUserCommand>
    {
        public BanUserCommandValidator()
        {
            RuleFor(x => x.TargetUserId)
                .NotEmpty().WithMessage("Target user ID is required.");

            RuleFor(x => x.AdminId)
                .NotEmpty().WithMessage("Admin ID is required.");

            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Reason for ban must be provided.");
        }
    }
}
