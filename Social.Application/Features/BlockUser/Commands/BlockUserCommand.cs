using AutoMapper;
using MediatR;
using Social.Application.Features.BlockUser.DTOs;
using Social.Application.Features.BlockUser.Requests;
using Social.Core.Interfaces;

namespace Social.Application.Features.BlockUser.Commands
{
    public record BlockUserCommand(BlockUserRequest blockUserRequest, string blockerId) : IRequest;

    public class BlockUserCommandHandler : IRequestHandler<BlockUserCommand>
    {
        private readonly IBlockUserRepository _blockUserRepository;
        private readonly IUserRepository _userRepository;

        public BlockUserCommandHandler(IBlockUserRepository blockUserRepository, IUserRepository userRepository)
        {
            _blockUserRepository = blockUserRepository;
            _userRepository = userRepository;
        }

        public async Task Handle(BlockUserCommand request, CancellationToken cancellationToken)
        {
            // Validate that the user is not trying to block themselves
            if (request.blockerId == request.blockUserRequest.BlockedUserId)
            {
                throw new InvalidOperationException("You cannot block yourself.");
            }

            // Validate that the blocked user exists
            var userToBlock = await _userRepository.GetUserByIdAsync(request.blockUserRequest.BlockedUserId);
            if (userToBlock == null)
            {
                throw new InvalidOperationException("The user you are trying to block does not exist.");
            }

            // Check if the user is already blocked
            var isAlreadyBlocked = await _blockUserRepository.IsUserBlockedAsync(request.blockerId, request.blockUserRequest.BlockedUserId);
            if (isAlreadyBlocked)
            {
                throw new InvalidOperationException("This user is already blocked.");
            }

            await _blockUserRepository.BlockUserAsync(request.blockerId, request.blockUserRequest.BlockedUserId);
        }
    }
}
