using FluentValidation;
using Social.Application.Features.Admin.Users.Commands;

namespace Social.Application.Features.Admin.Users.Validators
{
    public class UpdateUserRolesCommandValidator : AbstractValidator<UpdateUserRolesCommand>
    {
        public UpdateUserRolesCommandValidator()
        {
            RuleFor(x => x.TargetUserId)
                .NotEmpty().WithMessage("Target user ID is required.");

            RuleFor(x => x.AdminId)
                .NotEmpty().WithMessage("Admin ID is required.");

            RuleFor(x => x.Roles)
                .NotEmpty().WithMessage("At least one role must be specified.");
        }
    }
}
