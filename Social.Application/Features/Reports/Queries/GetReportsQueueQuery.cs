using MediatR;
using Social.Application.Features.Reports.DTOs;
using Social.Core.Interfaces;

namespace Social.Application.Features.Reports.Queries
{
    public record GetReportsQueueQuery(
        string? Status,
        int Page = 1,
        int PageSize = 20
    ) : IRequest<ReportsPageDto>;

    public class GetReportsQueueQueryHandler : IRequestHandler<GetReportsQueueQuery, ReportsPageDto>
    {
        private readonly IPostReportRepository _reports;

        public GetReportsQueueQueryHandler(IPostReportRepository reports)
        {
            _reports = reports;
        }

        public async Task<ReportsPageDto> Handle(GetReportsQueueQuery request, CancellationToken cancellationToken)
        {
            string? status = null;
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                status = ReportMapping.CanonicalStatus(request.Status)
                    ?? throw new ArgumentException(
                        $"Invalid report status '{request.Status}'. Expected 'Pending', 'Dismissed' or 'Actioned'.",
                        nameof(request.Status));
            }

            var (items, total) = await _reports.GetQueueAsync(status, request.Page, request.PageSize, cancellationToken);

            // One open-count lookup per distinct post (page-bounded, admin-only).
            var openCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var postId in items.Select(r => r.PostId).Distinct())
                openCounts[postId] = await _reports.GetOpenCountForPostAsync(postId, cancellationToken);

            return new ReportsPageDto
            {
                Items = items.Select(r => ReportMapping.ToDto(
                    r,
                    null,
                    null,
                    openCounts.TryGetValue(r.PostId, out var c) ? c : 0)).ToList(),
                TotalCount = total,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}
