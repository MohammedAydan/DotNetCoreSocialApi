using FluentValidation;
using MediatR;
using Social.Application.Features.Reports.DTOs;
using Social.Core.Entities;
using Social.Core.Interfaces;
using Social.Core.Reporting;

namespace Social.Application.Features.Reports.Commands
{
    public record ReportPostCommand(
        string PostId,
        string ReporterId,
        string Reason,
        string? Details
    ) : IRequest<PostReportDto>;

    public class ReportPostCommandHandler : IRequestHandler<ReportPostCommand, PostReportDto>
    {
        private readonly IAdminRepository _adminRepo;
        private readonly IPostReportRepository _reports;

        public ReportPostCommandHandler(IAdminRepository adminRepo, IPostReportRepository reports)
        {
            _adminRepo = adminRepo;
            _reports = reports;
        }

        public async Task<PostReportDto> Handle(ReportPostCommand request, CancellationToken cancellationToken)
        {
            // Unfiltered admin read by design: reporting is a safety signal, not
            // engagement, so block gates and audience checks must not suppress it.
            var post = await _adminRepo.GetPostByIdAsync(request.PostId, cancellationToken);
            if (post == null || post.IsDeleted)
                throw new KeyNotFoundException($"Post '{request.PostId}' was not found.");

            if (string.Equals(post.UserId, request.ReporterId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("You cannot report your own post.");

            if (await _reports.ExistsOpenAsync(request.PostId, request.ReporterId, cancellationToken))
                throw new InvalidOperationException("You have already reported this post. Your report is pending review.");

            var reason = ReportMapping.CanonicalReason(request.Reason)
                ?? throw new ArgumentException($"Invalid report reason '{request.Reason}'.", nameof(request.Reason));

            var report = new PostReport
            {
                Id = Guid.NewGuid().ToString(),
                PostId = request.PostId,
                ReporterUserId = request.ReporterId,
                Reason = reason,
                Details = request.Details?.Trim(),
                Status = ReportStatuses.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _reports.AddAsync(report, cancellationToken);

            return ReportMapping.ToDto(report, post.Content, post.UserId);
        }
    }
}
