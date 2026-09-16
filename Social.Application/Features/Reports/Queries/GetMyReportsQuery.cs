using MediatR;
using Social.Application.Features.Reports.DTOs;
using Social.Core.Interfaces;

namespace Social.Application.Features.Reports.Queries
{
    public record GetMyReportsQuery(
        string ReporterId,
        int Page = 1,
        int Limit = 20
    ) : IRequest<ReportsPageDto>;

    public class GetMyReportsQueryHandler : IRequestHandler<GetMyReportsQuery, ReportsPageDto>
    {
        private readonly IPostReportRepository _reports;

        public GetMyReportsQueryHandler(IPostReportRepository reports)
        {
            _reports = reports;
        }

        public async Task<ReportsPageDto> Handle(GetMyReportsQuery request, CancellationToken cancellationToken)
        {
            // Repository throws on page < 1 and clamps limit to 50; surface loudly.
            var (items, total) = await _reports.GetMyReportsAsync(
                request.ReporterId, request.Page, request.Limit, cancellationToken);

            return new ReportsPageDto
            {
                Items = items.Select(r => ReportMapping.ToDto(r)).ToList(),
                TotalCount = total,
                Page = request.Page,
                PageSize = request.Limit
            };
        }
    }
}
