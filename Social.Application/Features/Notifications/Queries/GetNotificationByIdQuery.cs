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
    public record GetNotificationByIdQuery(string Id) : IRequest<NotificationDto>;

    public class GetNotificationByIdQueryHandler : IRequestHandler<GetNotificationByIdQuery, NotificationDto>
    {
        private readonly INotificationRepository _repo;

        public GetNotificationByIdQueryHandler(INotificationRepository repo)
        {
            _repo = repo;
        }

        public async Task<NotificationDto> Handle(GetNotificationByIdQuery request, CancellationToken cancellationToken)
        {
            var notification = await _repo.GetByIdAsync(request.Id);

            if (notification == null)
                throw new KeyNotFoundException("Notification not found.");

            return new NotificationDto
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
            };
        }
    }
}
