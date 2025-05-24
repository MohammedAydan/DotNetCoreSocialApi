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
    public record GetFollowingQuery(string userId, int page = 1, int limit = 20) : IRequest<IEnumerable<FollowerDto>>;

    public class GetFollowingQueryHandler : IRequestHandler<GetFollowingQuery, IEnumerable<FollowerDto>>
    {
        private readonly IFollowRepository _followRepository;
        private readonly IMapper _mapper;
        public GetFollowingQueryHandler(IFollowRepository followRepository, IMapper mapper)
        {
            _followRepository = followRepository;
            _mapper = mapper;
        }
        public async Task<IEnumerable<FollowerDto>> Handle(GetFollowingQuery request, CancellationToken cancellationToken)
        {
            var following = await _followRepository.GetFollowingAsync(request.userId, request.page, request.limit);
            if (following == null || !following.Any())
            {
                return Enumerable.Empty<FollowerDto>();
            }
            var followingDtos = _mapper.Map<IEnumerable<FollowerDto>>(following);
            return followingDtos;
        }
    }
}
