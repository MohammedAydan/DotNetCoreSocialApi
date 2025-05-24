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
    public record AcceptFollowCommand(FollowRequest followRequest) : IRequest<bool>;

    public class AcceptFollowCommandHandler : IRequestHandler<AcceptFollowCommand, bool>
    {
        private readonly IFollowRepository _followRepository;
        public AcceptFollowCommandHandler(IFollowRepository followRepository)
        {
            _followRepository = followRepository;
        }
        public async Task<bool> Handle(AcceptFollowCommand request, CancellationToken cancellationToken)
        {
            var result = await _followRepository.AcceptFollowRequestAsync(
                request.followRequest.TargetUserId,
                request.followRequest.FollowerId
                );

            return result;
        }
    }
}
