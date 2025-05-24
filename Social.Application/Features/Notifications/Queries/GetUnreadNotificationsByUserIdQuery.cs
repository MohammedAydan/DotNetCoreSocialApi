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
    public record GetUnreadNotificationsByUserIdQuery(string UserId, int Page, int Limit) : IRequest<IEnumerable<NotificationDto>>;

    public class GetUnreadNotificationsByUserIdQueryHandler : IRequestHandler<GetUnreadNotificationsByUserIdQuery, IEnumerable<NotificationDto>>
    {
        private readonly INotificationRepository _repo;

        public GetUnreadNotificationsByUserIdQueryHandler(INotificationRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<NotificationDto>> Handle(GetUnreadNotificationsByUserIdQuery request, CancellationToken cancellationToken)
        {
            var notifications = await _repo.GetUnreadByUserIdAsync(request.UserId, request.Page, request.Limit);

            return notifications.Select(notification => new NotificationDto
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
        }
    }
}
