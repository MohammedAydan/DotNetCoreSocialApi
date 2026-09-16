using FluentValidation;
using Social.Application.Features.Reports.Commands;

namespace Social.Application.Features.Reports.Validators
{
    public class ResolveReportCommandValidator : AbstractValidator<ResolveReportCommand>
    {
        public ResolveReportCommandValidator()
        {
            RuleFor(x => x.ReportId)
                .NotEmpty().WithMessage("Report ID is required.");

            RuleFor(x => x.AdminId)
                .NotEmpty().WithMessage("Admin ID is required.");

            RuleFor(x => x.Action)
                .NotEmpty().WithMessage("Action is required.")
                .Must(BeKnownAction).WithMessage("Action must be one of: dismiss, hide_post.");

            RuleFor(x => x.Note)
                .MaximumLength(500).WithMessage("Note must be at most 500 characters.");
        }

        private static bool BeKnownAction(string? action)
        {
            if (string.IsNullOrWhiteSpace(action))
                return false;
            var normalized = action.Trim().ToLowerInvariant();
            return normalized == "dismiss" || normalized == "hide_post";
        }
    }
}
