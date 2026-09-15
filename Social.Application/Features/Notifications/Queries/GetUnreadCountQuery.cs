using MediatR;
using Social.Core.Interfaces;

namespace Social.Application.Features.Notifications.Queries
{
    public record GetUnreadCountQuery(string UserId) : IRequest<int>;

    public class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, int>
    {
        private readonly INotificationRepository _repo;

        public GetUnreadCountQueryHandler(INotificationRepository repo)
        {
            _repo = repo;
        }

        public async Task<int> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
        {
            var preference = await _repo.GetPreferenceAsync(request.UserId, cancellationToken);
            if (preference == null || !preference.IsQuietNow(DateTime.UtcNow))
                await _repo.ReleaseDeferredAsync(request.UserId, cancellationToken);

            return await _repo.GetUnreadCountAsync(request.UserId, cancellationToken);
        }
    }
}
