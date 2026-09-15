using FluentValidation;
using Social.Application.Features.Admin.Users.Commands;

namespace Social.Application.Features.Admin.Users.Validators
{
    public class AdminResetPasswordCommandValidator : AbstractValidator<AdminResetPasswordCommand>
    {
        public AdminResetPasswordCommandValidator()
        {
            RuleFor(x => x.TargetUserId)
                .NotEmpty().WithMessage("Target user ID is required.");

            RuleFor(x => x.AdminId)
                .NotEmpty().WithMessage("Admin ID is required.");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("New password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");
        }
    }
}
