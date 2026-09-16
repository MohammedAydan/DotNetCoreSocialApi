using MediatR;
using Social.Core.Interfaces;
using Social.Core.Reporting;

namespace Social.Application.Features.Reports.Commands
{
    public record CancelReportCommand(
        string ReportId,
        string ReporterId
    ) : IRequest<bool>;

    public class CancelReportCommandHandler : IRequestHandler<CancelReportCommand, bool>
    {
        private readonly IPostReportRepository _reports;

        public CancelReportCommandHandler(IPostReportRepository reports)
        {
            _reports = reports;
        }

        public async Task<bool> Handle(CancelReportCommand request, CancellationToken cancellationToken)
        {
            var report = await _reports.GetByIdAsync(request.ReportId, cancellationToken)
                ?? throw new KeyNotFoundException($"Report '{request.ReportId}' was not found.");

            if (!string.Equals(report.ReporterUserId, request.ReporterId, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("You can only cancel your own reports.");

            if (!string.Equals(report.Status, ReportStatuses.Pending, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only pending reports can be cancelled.");

            // Reports are audit-light rows, not posts: hard delete is allowed.
            await _reports.DeleteAsync(request.ReportId, cancellationToken);
            return true;
        }
    }
}
