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
    public record RejectFollowCommand(FollowRequest followRequest) : IRequest<bool>;

    public class RejectFollowCommandHandler : IRequestHandler<RejectFollowCommand, bool>
    {
        private readonly IFollowRepository _followRepository;
        public RejectFollowCommandHandler(IFollowRepository followRepository)
        {
            _followRepository = followRepository;
        }
        public async Task<bool> Handle(RejectFollowCommand request, CancellationToken cancellationToken)
        {
            var result = await _followRepository.RejectFollowRequestAsync(
                request.followRequest.TargetUserId,
                request.followRequest.FollowerId
                );
            return result;
        }
    }
}
