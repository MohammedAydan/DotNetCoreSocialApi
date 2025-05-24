using MediatR;
using Social.Application.Features.Notifications.DTOs;
using Social.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Notifications.Commands
{
    public record UpdateNotificationCommand(UpdateNotificationDto Dto) : IRequest<NotificationDto>;

    public class UpdateNotificationCommandHandler : IRequestHandler<UpdateNotificationCommand, NotificationDto>
    {
        private readonly INotificationRepository _repo;

        public UpdateNotificationCommandHandler(INotificationRepository repo)
        {
            _repo = repo;
        }

        public async Task<NotificationDto> Handle(UpdateNotificationCommand request, CancellationToken cancellationToken)
        {
            var existingNotification = await _repo.GetByIdAsync(request.Dto.Id);

            if (existingNotification == null)
                throw new KeyNotFoundException("Notification not found.");

            existingNotification.Type = request.Dto.Type;
            existingNotification.Message = request.Dto.Message;
            existingNotification.PostId = request.Dto.PostId;
            existingNotification.CommentId = request.Dto.CommentId;
            existingNotification.FollowerId = request.Dto.FollowerId;
            existingNotification.LikeId = request.Dto.LikeId;
            existingNotification.ImageUrl = request.Dto.ImageUrl;

            await _repo.UpdateAsync(existingNotification);

            return new NotificationDto
            {
                Id = existingNotification.Id,
                UserId = existingNotification.UserId,
                Type = existingNotification.Type,
                Message = existingNotification.Message,
                PostId = existingNotification.PostId,
                CommentId = existingNotification.CommentId,
                FollowerId = existingNotification.FollowerId,
                LikeId = existingNotification.LikeId,
                ImageUrl = existingNotification.ImageUrl,
                IsRead = existingNotification.IsRead,
                CreatedAt = existingNotification.CreatedAt
            };
        }
    }
}
