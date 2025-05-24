using AutoMapper;
using MediatR;
using Social.Application.Features.Followers.DTOs;
using Social.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Follow.Queries
{
    public record GetPendingFollowRequestQuery(string userId, int page = 1, int limit = 20) : IRequest<IEnumerable<FollowerDto>>;

    public class GetPendingFollowRequestQueryHandler : IRequestHandler<GetPendingFollowRequestQuery, IEnumerable<FollowerDto>>
    {
        private readonly IFollowRepository _followRepository;
        private readonly IMapper _mapper;
        public GetPendingFollowRequestQueryHandler(IFollowRepository followRepository, IMapper mapper)
        {
            _followRepository = followRepository;
            _mapper = mapper;
        }
        public async Task<IEnumerable<FollowerDto>> Handle(GetPendingFollowRequestQuery request, CancellationToken cancellationToken)
        {
            var pendingRequests = await _followRepository.GetPendingFollowRequestsAsync(request.userId, request.page, request.limit);
            if (pendingRequests == null || !pendingRequests.Any())
            {
                return Enumerable.Empty<FollowerDto>();
            }
            var pendingRequestDtos = _mapper.Map<IEnumerable<FollowerDto>>(pendingRequests);
            return pendingRequestDtos;
        }
    }
}
