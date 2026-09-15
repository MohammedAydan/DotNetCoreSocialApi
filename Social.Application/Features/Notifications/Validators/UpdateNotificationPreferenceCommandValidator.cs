using FluentValidation;
using Social.Application.Features.Notifications.Commands;

namespace Social.Application.Features.Notifications.Validators
{
    public class UpdateNotificationPreferenceCommandValidator : AbstractValidator<UpdateNotificationPreferenceCommand>
    {
        public UpdateNotificationPreferenceCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required.");

            RuleFor(x => x.Dto)
                .NotNull().WithMessage("Preference payload is required.");

            When(x => x.Dto != null, () =>
            {
                RuleFor(x => x.Dto.QuietStartHourUtc)
                    .Must(h => !h.HasValue || (h.Value >= 0 && h.Value <= 23))
                    .WithMessage("QuietStartHourUtc must be between 0 and 23.");
                RuleFor(x => x.Dto.QuietEndHourUtc)
                    .Must(h => !h.HasValue || (h.Value >= 0 && h.Value <= 23))
                    .WithMessage("QuietEndHourUtc must be between 0 and 23.");
                RuleFor(x => x.Dto)
                    .Must(d => (d.QuietStartHourUtc.HasValue && d.QuietEndHourUtc.HasValue) ||
                               (!d.QuietStartHourUtc.HasValue && !d.QuietEndHourUtc.HasValue))
                    .WithMessage("Quiet hours require both start and end, or neither.");
                RuleFor(x => x.Dto)
                    .Must(d => !d.QuietStartHourUtc.HasValue || !d.QuietEndHourUtc.HasValue ||
                               d.QuietStartHourUtc.Value != d.QuietEndHourUtc.Value)
                    .WithMessage("Quiet hours start and end must differ.");
            });
        }
    }
}
