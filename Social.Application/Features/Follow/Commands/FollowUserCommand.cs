using AutoMapper;
using MediatR;
using Social.Application.Features.Follow.DTOs;
using Social.Application.Features.Followers.DTOs;
using Social.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Follow.Commands
{
    public record FollowUserCommand(FollowRequest followRequest) : IRequest<FollowerDto>;

    public class FollowUserCommandHandler : IRequestHandler<FollowUserCommand, FollowerDto>
    {
        private readonly IFollowRepository _followRepository;
        private readonly IMapper _mapper;

        public FollowUserCommandHandler(IFollowRepository followRepository, IMapper mapper)
        {
            _followRepository = followRepository;
            _mapper = mapper;
        }
        public async Task<FollowerDto> Handle(FollowUserCommand request, CancellationToken cancellationToken)
        {
            var follower = await _followRepository.FollowUserAsync(request.followRequest.FollowerId, request.followRequest.TargetUserId);
            if(follower == null) {
                throw new InvalidOperationException("Follow operation failed.");
            }

            var followerDto = _mapper.Map<FollowerDto>(follower);

            return followerDto;
        }
    }
}
