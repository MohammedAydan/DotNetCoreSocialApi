using MediatR;
using Social.Application.Features.Notifications.DTOs;
using Social.Core.Entities;
using Social.Core.Interfaces;

namespace Social.Application.Features.Notifications.Commands
{
    public record UpdateNotificationPreferenceCommand(string UserId, NotificationPreferenceDto Dto) : IRequest<NotificationPreferenceDto>;

    public class UpdateNotificationPreferenceCommandHandler : IRequestHandler<UpdateNotificationPreferenceCommand, NotificationPreferenceDto>
    {
        private readonly INotificationRepository _repo;

        public UpdateNotificationPreferenceCommandHandler(INotificationRepository repo)
        {
            _repo = repo;
        }

        public async Task<NotificationPreferenceDto> Handle(UpdateNotificationPreferenceCommand request, CancellationToken cancellationToken)
        {
            if (request.Dto == null)
                throw new ArgumentException("Preference payload is required.");

            var entity = new NotificationPreference
            {
                UserId = request.UserId,
                LikeEnabled = request.Dto.LikeEnabled,
                CommentEnabled = request.Dto.CommentEnabled,
                ReplyEnabled = request.Dto.ReplyEnabled,
                FollowEnabled = request.Dto.FollowEnabled,
                FollowRequestEnabled = request.Dto.FollowRequestEnabled,
                ShareEnabled = request.Dto.ShareEnabled,
                MentionEnabled = request.Dto.MentionEnabled,
                QuietStartHourUtc = request.Dto.QuietStartHourUtc,
                QuietEndHourUtc = request.Dto.QuietEndHourUtc,
                DigestEnabled = request.Dto.DigestEnabled
            };

            var saved = await _repo.UpsertPreferenceAsync(entity, cancellationToken);

            return new NotificationPreferenceDto
            {
                UserId = saved.UserId,
                LikeEnabled = saved.LikeEnabled,
                CommentEnabled = saved.CommentEnabled,
                ReplyEnabled = saved.ReplyEnabled,
                FollowEnabled = saved.FollowEnabled,
                FollowRequestEnabled = saved.FollowRequestEnabled,
                ShareEnabled = saved.ShareEnabled,
                MentionEnabled = saved.MentionEnabled,
                QuietStartHourUtc = saved.QuietStartHourUtc,
                QuietEndHourUtc = saved.QuietEndHourUtc,
                DigestEnabled = saved.DigestEnabled
            };
        }
    }
}
