using MediatR;
using Social.Application.Features.Notifications.DTOs;
using Social.Core.Interfaces;

namespace Social.Application.Features.Notifications.Queries
{
    public record GetInboxQuery(string UserId, string? Type = null, bool UnreadOnly = false, int Page = 1, int Limit = 20)
        : IRequest<(IEnumerable<NotificationDto> Items, int TotalCount, int UnreadCount)>;

    public class GetInboxQueryHandler : IRequestHandler<GetInboxQuery, (IEnumerable<NotificationDto>, int, int)>
    {
        private readonly INotificationRepository _repo;

        public GetInboxQueryHandler(INotificationRepository repo)
        {
            _repo = repo;
        }

        public async Task<(IEnumerable<NotificationDto>, int, int)> Handle(GetInboxQuery request, CancellationToken cancellationToken)
        {
            // Auto-release quiet-hours rows once the window has passed.
            var preference = await _repo.GetPreferenceAsync(request.UserId, cancellationToken);
            if (preference == null || !preference.IsQuietNow(DateTime.UtcNow))
                await _repo.ReleaseDeferredAsync(request.UserId, cancellationToken);

            var (notifications, total) = await _repo.GetInboxAsync(
                request.UserId, request.Type, request.UnreadOnly, request.Page, request.Limit, cancellationToken);
            var unreadCount = await _repo.GetUnreadCountAsync(request.UserId, cancellationToken);

            var dtos = notifications.Select(n => new NotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                RecipientId = n.RecipientId,
                Type = n.Type,
                Message = n.Message,
                PostId = n.PostId,
                CommentId = n.CommentId,
                FollowerId = n.FollowerId,
                LikeId = n.LikeId,
                ImageUrl = n.ImageUrl,
                IsRead = n.IsRead,
                IsDeferred = n.IsDeferred,
                GroupKey = n.GroupKey,
                ActorCount = n.ActorCount,
                LastActorName = n.LastActorName,
                Priority = n.Priority,
                CreatedAt = n.CreatedAt
            });

            return (dtos, total, unreadCount);
        }
    }
}
