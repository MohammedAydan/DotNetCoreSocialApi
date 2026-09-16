using MediatR;
using Social.Application.Features.Reports.DTOs;
using Social.Core.Interfaces;

namespace Social.Application.Features.Reports.Queries
{
    public record GetReportByIdQuery(
        string ReportId
    ) : IRequest<PostReportDto>;

    public class GetReportByIdQueryHandler : IRequestHandler<GetReportByIdQuery, PostReportDto>
    {
        private readonly IPostReportRepository _reports;

        public GetReportByIdQueryHandler(IPostReportRepository reports)
        {
            _reports = reports;
        }

        public async Task<PostReportDto> Handle(GetReportByIdQuery request, CancellationToken cancellationToken)
        {
            var report = await _reports.GetByIdAsync(request.ReportId, cancellationToken)
                ?? throw new KeyNotFoundException($"Report '{request.ReportId}' was not found.");

            var openCount = await _reports.GetOpenCountForPostAsync(report.PostId, cancellationToken);
            return ReportMapping.ToDto(report, null, null, openCount);
        }
    }
}
