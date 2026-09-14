using MediatR;
using Social.Core.Interfaces;

namespace Social.Application.Features.Users.Commands
{
    public record DeleteUserCommand(string userId) : IRequest<bool>;

    public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, bool>
    {
        private readonly IUserRepository _userRepository;

        public DeleteUserCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        public async Task<bool> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.userId))
            {
                throw new ArgumentNullException(nameof(request.userId), "User ID cannot be null or empty");
            }
            return await _userRepository.DeleteUserAsync(request.userId);
        }
    }
}
