using MediatR;
using Social.Application.Features.Admin.Common;
using Social.Application.Features.Admin.Users.DTOs;
using Social.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Social.Application.Features.Admin.Users.Queries
{
    public record GetAdminUsersQuery(
        int Page = 1,
        int PageSize = 10,
        string? Q = null
    ) : IRequest<PaginatedResultDto<AdminUserDto>>;

    public class GetAdminUsersQueryHandler : IRequestHandler<GetAdminUsersQuery, PaginatedResultDto<AdminUserDto>>
    {
        private readonly IAdminRepository _adminRepo;

        public GetAdminUsersQueryHandler(IAdminRepository adminRepo)
        {
            _adminRepo = adminRepo;
        }

        public async Task<PaginatedResultDto<AdminUserDto>> Handle(GetAdminUsersQuery request, CancellationToken cancellationToken)
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);

            var (users, totalCount) = await _adminRepo.GetUsersPagedAsync(page, pageSize, request.Q, cancellationToken);

            var dtos = new List<AdminUserDto>();
            foreach (var u in users)
            {
                var roles = await _adminRepo.GetUserRolesAsync(u, cancellationToken);
                var isLockedOut = u.LockoutEnabled && u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow;

                dtos.Add(new AdminUserDto
                {
                    Id = u.Id,
                    UserName = u.UserName ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Roles = roles.ToList(),
                    IsVerified = u.IsVerified,
                    IsLockedOut = isLockedOut,
                    LockoutEnd = u.LockoutEnd,
                    CreatedAt = u.CreatedAt,
                    PostsCount = u.PostsCount,
                    FollowersCount = u.FollowersCount
                });
            }

            return new PaginatedResultDto<AdminUserDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}
