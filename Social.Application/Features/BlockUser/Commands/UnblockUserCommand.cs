using MediatR;
using Social.Application.Features.BlockUser.Requests;
using Social.Core.Interfaces;

namespace Social.Application.Features.BlockUser.Commands
{
    public record UnblockUserCommand(BlockUserRequest blockUserRequest, string blockerId) : IRequest;

    public class UnblockUserCommandHandler : IRequestHandler<UnblockUserCommand>
    {
        private readonly IBlockUserRepository _blockUserRepository;
        private readonly IUserRepository _userRepository;

        public UnblockUserCommandHandler(IBlockUserRepository blockUserRepository, IUserRepository userRepository)
        {
            _blockUserRepository = blockUserRepository;
            _userRepository = userRepository;
        }

        public async Task Handle(UnblockUserCommand request, CancellationToken cancellationToken)
        {
            // Validate that the user exists
            var userToUnblock = await _userRepository.GetUserByIdAsync(request.blockUserRequest.BlockedUserId);
            if (userToUnblock == null)
            {
                throw new InvalidOperationException("The user you are trying to unblock does not exist.");
            }

            // Check if the user is actually blocked
            var isBlocked = await _blockUserRepository.IsUserBlockedAsync(request.blockerId, request.blockUserRequest.BlockedUserId);
            if (!isBlocked)
            {
                throw new InvalidOperationException("This user is not blocked.");
            }

            await _blockUserRepository.UnblockUserAsync(request.blockerId, request.blockUserRequest.BlockedUserId);
        }
    }
}
