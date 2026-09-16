using FluentValidation;
using Social.Application.Features.Reports.Commands;
using Social.Core.Reporting;

namespace Social.Application.Features.Reports.Validators
{
    public class ReportPostCommandValidator : AbstractValidator<ReportPostCommand>
    {
        public ReportPostCommandValidator()
        {
            RuleFor(x => x.PostId)
                .NotEmpty().WithMessage("Post ID is required.");

            RuleFor(x => x.ReporterId)
                .NotEmpty().WithMessage("Reporter ID is required.");

            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Reason is required.")
                .Must(BeKnownReason).WithMessage(
                    $"Reason must be one of: {ReportReasons.Spam}, {ReportReasons.Harassment}, " +
                    $"{ReportReasons.HateSpeech}, {ReportReasons.Nudity}, {ReportReasons.Violence}, " +
                    $"{ReportReasons.Misinformation}, {ReportReasons.Copyright}, {ReportReasons.Other}.");

            RuleFor(x => x.Details)
                .MaximumLength(1000).WithMessage("Details must be at most 1000 characters.");

            When(x => string.Equals(x.Reason?.Trim(), ReportReasons.Other, StringComparison.OrdinalIgnoreCase), () =>
            {
                RuleFor(x => x.Details)
                    .NotEmpty().WithMessage("Details are required when reason is 'Other'.");
            });
        }

        private static bool BeKnownReason(string? reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return false;
            return ReportMapping.ValidReasons.Any(r =>
                string.Equals(r, reason.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }
}
