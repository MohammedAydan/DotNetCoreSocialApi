using AutoMapper;
using MediatR;
using Social.Application.Features.Follow.DTOs;
using Social.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Follow.Commands
{
    public record UnfollowUserCommand(FollowRequest followRequest) : IRequest<bool>;

    public class UnfollowUserCommandHandler : IRequestHandler<UnfollowUserCommand, bool>
    {
        private readonly IFollowRepository _followRepository;

        public UnfollowUserCommandHandler(IFollowRepository followRepository)
        {
            _followRepository = followRepository;
        }

        public async Task<bool> Handle(UnfollowUserCommand request, CancellationToken cancellationToken)
        {
            var result = await _followRepository.UnfollowUserAsync(request.followRequest.FollowerId, request.followRequest.TargetUserId);
            return result;
        }
    }
}
