using MediatR;
using Social.Core.Interfaces;

namespace Social.Application.Features.BlockUser.Queries
{
    public record IsUserBlockedQuery(string UserId, string BlockedUserId) : IRequest<bool>;

    public class IsUserBlockedQueryHandler : IRequestHandler<IsUserBlockedQuery, bool>
    {
        private readonly IBlockUserRepository blockUserRepository;

        public IsUserBlockedQueryHandler(IBlockUserRepository blockUserRepository)
        {
            this.blockUserRepository = blockUserRepository;
        }

        public async Task<bool> Handle(IsUserBlockedQuery request, CancellationToken cancellationToken)
        {
            return await blockUserRepository.IsUserBlockedAsync(request.UserId, request.BlockedUserId);
        }
    }
}