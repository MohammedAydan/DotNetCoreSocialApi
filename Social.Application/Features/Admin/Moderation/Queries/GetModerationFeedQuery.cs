using MediatR;
using Social.Application.Features.Admin.Common;
using Social.Application.Features.Admin.Moderation.DTOs;
using Social.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Social.Application.Features.Admin.Moderation.Queries
{
    public record GetModerationFeedQuery(
        int Page = 1,
        int PageSize = 20
    ) : IRequest<PaginatedResultDto<AdminModerationItemDto>>;

    public class GetModerationFeedQueryHandler : IRequestHandler<GetModerationFeedQuery, PaginatedResultDto<AdminModerationItemDto>>
    {
        private readonly IAdminRepository _adminRepo;

        public GetModerationFeedQueryHandler(IAdminRepository adminRepo)
        {
            _adminRepo = adminRepo;
        }

        public async Task<PaginatedResultDto<AdminModerationItemDto>> Handle(GetModerationFeedQuery request, CancellationToken cancellationToken)
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);

            var (items, totalCount) = await _adminRepo.GetModerationFeedAsync(page, pageSize, cancellationToken);

            var dtos = items.Select(i => new AdminModerationItemDto
            {
                Id = i.Id,
                Type = i.Type,
                Title = i.Title,
                Content = i.Content,
                AuthorId = i.AuthorId,
                AuthorUserName = i.AuthorUserName,
                AuthorEmail = i.AuthorEmail,
                Visibility = i.Visibility,
                IsDeleted = i.IsDeleted,
                LikesCount = i.LikesCount,
                CommentsCount = i.CommentsCount,
                SharesCount = i.SharesCount,
                CreatedAt = i.CreatedAt,
                Media = i.Media?.Select(m => new AdminMediaDto
                {
                    Id = m.Id,
                    Url = m.Url,
                    Type = m.Type,
                    ThumbnailUrl = m.ThumbnailUrl
                }).ToList() ?? new List<AdminMediaDto>()
            }).ToList();

            return new PaginatedResultDto<AdminModerationItemDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}
