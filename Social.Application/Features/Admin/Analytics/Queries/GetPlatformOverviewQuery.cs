using MediatR;
using Social.Application.Features.Admin.Analytics.DTOs;
using Social.Core.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace Social.Application.Features.Admin.Analytics.Queries
{
    public record GetPlatformOverviewQuery : IRequest<PlatformOverviewMetricsDto>;

    public class GetPlatformOverviewQueryHandler : IRequestHandler<GetPlatformOverviewQuery, PlatformOverviewMetricsDto>
    {
        private readonly IAdminRepository _adminRepo;

        public GetPlatformOverviewQueryHandler(IAdminRepository adminRepo)
        {
            _adminRepo = adminRepo;
        }

        public async Task<PlatformOverviewMetricsDto> Handle(GetPlatformOverviewQuery request, CancellationToken cancellationToken)
        {
            var metrics = await _adminRepo.GetOverviewMetricsAsync(cancellationToken);
            return new PlatformOverviewMetricsDto
            {
                TotalUsers = metrics.TotalUsers,
                ActiveUsers24h = metrics.ActiveUsers24h,
                TotalPosts = metrics.TotalPosts,
                TotalComments = metrics.TotalComments,
                TotalLikes = metrics.TotalLikes
            };
        }
    }
}
