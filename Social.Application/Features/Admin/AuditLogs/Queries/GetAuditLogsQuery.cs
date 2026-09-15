using MediatR;
using Social.Application.Features.Admin.AuditLogs.DTOs;
using Social.Application.Features.Admin.Common;
using Social.Core.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Social.Application.Features.Admin.AuditLogs.Queries
{
    public record GetAuditLogsQuery(
        int Page = 1,
        int PageSize = 20,
        string? ActionType = null,
        string? AdminId = null,
        DateTime? FromDate = null,
        DateTime? ToDate = null
    ) : IRequest<PaginatedResultDto<AuditLogDto>>;

    public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PaginatedResultDto<AuditLogDto>>
    {
        private readonly IAuditLogRepository _auditRepo;

        public GetAuditLogsQueryHandler(IAuditLogRepository auditRepo)
        {
            _auditRepo = auditRepo;
        }

        public async Task<PaginatedResultDto<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
        {
            if (request.FromDate.HasValue && request.ToDate.HasValue && request.FromDate.Value > request.ToDate.Value)
            {
                throw new ArgumentException("FromDate cannot be later than ToDate.");
            }

            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);

            var (logs, totalCount) = await _auditRepo.GetPagedAsync(
                page,
                pageSize,
                request.ActionType,
                request.AdminId,
                request.FromDate,
                request.ToDate,
                cancellationToken);

            var dtos = logs.Select(l => new AuditLogDto
            {
                Id = l.Id,
                AdminId = l.AdminId,
                AdminEmail = l.AdminEmail,
                ActionType = l.ActionType,
                TargetEntity = l.TargetEntity,
                TargetId = l.TargetId,
                Reason = l.Reason,
                TimestampUtc = l.TimestampUtc
            }).ToList();

            return new PaginatedResultDto<AuditLogDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}
