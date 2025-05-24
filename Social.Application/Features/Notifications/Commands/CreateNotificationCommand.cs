using MediatR;
using Social.Application.Features.Notifications.DTOs;
using Social.Core.Entities;
using Social.Core.Interfaces;

namespace Social.Application.Features.Notifications.Commands
{
    public record CreateNotificationCommand(CreateNotificationDto Dto) : IRequest<NotificationDto>;

    public class CreateNotificationCommandHandler : IRequestHandler<CreateNotificationCommand, NotificationDto>
    {
        private readonly INotificationRepository _repo;

        public CreateNotificationCommandHandler(INotificationRepository repo)
        {
            _repo = repo;
        }

        public async Task<NotificationDto> Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
        {
            var entity = new Notification
            {
                Id = Guid.NewGuid().ToString(),
                UserId = request.Dto.UserId,
                Type = request.Dto.Type,
                Message = request.Dto.Message,
                PostId = request.Dto.PostId,
                CommentId = request.Dto.CommentId,
                FollowerId = request.Dto.FollowerId,
                LikeId = request.Dto.LikeId,
                ImageUrl = request.Dto.ImageUrl,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(entity);

            return new NotificationDto
            {
                Id = entity.Id,
                UserId = entity.UserId,
                Type = entity.Type,
                Message = entity.Message,
                PostId = entity.PostId,
                CommentId = entity.CommentId,
                FollowerId = entity.FollowerId,
                LikeId = entity.LikeId,
                ImageUrl = entity.ImageUrl,
                IsRead = entity.IsRead,
                CreatedAt = entity.CreatedAt
            };
        }
    }

}
