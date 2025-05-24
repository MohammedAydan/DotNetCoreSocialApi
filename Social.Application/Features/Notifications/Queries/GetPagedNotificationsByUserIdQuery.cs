using MediatR;
using Social.Application.Features.Notifications.DTOs;
using Social.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Notifications.Queries
{
    public record GetPagedNotificationsByUserIdQuery(string UserId, int Page, int Limit) : IRequest<(IEnumerable<NotificationDto>, int)>;

    public class GetPagedNotificationsByUserIdQueryHandler : IRequestHandler<GetPagedNotificationsByUserIdQuery, (IEnumerable<NotificationDto>, int)>
    {
        private readonly INotificationRepository _repo;

        public GetPagedNotificationsByUserIdQueryHandler(INotificationRepository repo)
        {
            _repo = repo;
        }

        public async Task<(IEnumerable<NotificationDto>, int)> Handle(GetPagedNotificationsByUserIdQuery request, CancellationToken cancellationToken)
        {
            var (notifications, totalCount) = await _repo.GetPagedByUserIdAsync(request.UserId, request.Page, request.Limit);

            var notificationDtos = notifications.Select(notification => new NotificationDto
            {
                Id = notification.Id,
                UserId = notification.UserId,
                Type = notification.Type,
                Message = notification.Message,
                PostId = notification.PostId,
                CommentId = notification.CommentId,
                FollowerId = notification.FollowerId,
                LikeId = notification.LikeId,
                ImageUrl = notification.ImageUrl,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt
            });

            return (notificationDtos, totalCount);
        }
    }
}
