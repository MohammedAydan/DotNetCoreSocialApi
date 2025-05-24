using MediatR;
using Social.Core.Interfaces;

namespace Social.Application.Features.Users.Commends
{
    public record DeleteUserCommand(string userId) : IRequest<bool>;

    public class DeleteUserCommendHandler : IRequestHandler<DeleteUserCommand, bool>
    {
        private readonly IUserRepository _userRepository;

        public DeleteUserCommendHandler(IUserRepository userRepository)
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
