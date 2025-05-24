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
    public record GetFollowersQuery(string userId, int page = 1, int limit = 20) : IRequest<IEnumerable<FollowerDto>>;

    public class GetFollowersQueryHandler : IRequestHandler<GetFollowersQuery, IEnumerable<FollowerDto>>
    {
        private readonly IFollowRepository _followRepository;
        private readonly IMapper _mapper;

        public GetFollowersQueryHandler(IFollowRepository followRepository, IMapper mapper)
        {
            _followRepository = followRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<FollowerDto>> Handle(GetFollowersQuery request, CancellationToken cancellationToken)
        {
            var followers = await _followRepository.GetFollowersAsync(request.userId, request.page, request.limit);
            if (followers == null || !followers.Any())
            {
                return Enumerable.Empty<FollowerDto>();
            }

            var followerDtos = _mapper.Map<IEnumerable<FollowerDto>>(followers);
            return followerDtos;
        }
    }
}
