using MediatR;
using Social.Application.Features.Notifications.DTOs;
using Social.Core.Interfaces;

namespace Social.Application.Features.Notifications.Queries
{
    public record GetNotificationPreferenceQuery(string UserId) : IRequest<NotificationPreferenceDto>;

    public class GetNotificationPreferenceQueryHandler : IRequestHandler<GetNotificationPreferenceQuery, NotificationPreferenceDto>
    {
        private readonly INotificationRepository _repo;

        public GetNotificationPreferenceQueryHandler(INotificationRepository repo)
        {
            _repo = repo;
        }

        public async Task<NotificationPreferenceDto> Handle(GetNotificationPreferenceQuery request, CancellationToken cancellationToken)
        {
            var preference = await _repo.GetPreferenceAsync(request.UserId, cancellationToken);
            if (preference == null)
                return new NotificationPreferenceDto { UserId = request.UserId };

            return new NotificationPreferenceDto
            {
                UserId = preference.UserId,
                LikeEnabled = preference.LikeEnabled,
                CommentEnabled = preference.CommentEnabled,
                ReplyEnabled = preference.ReplyEnabled,
                FollowEnabled = preference.FollowEnabled,
                FollowRequestEnabled = preference.FollowRequestEnabled,
                ShareEnabled = preference.ShareEnabled,
                MentionEnabled = preference.MentionEnabled,
                QuietStartHourUtc = preference.QuietStartHourUtc,
                QuietEndHourUtc = preference.QuietEndHourUtc,
                DigestEnabled = preference.DigestEnabled
            };
        }
    }
}
