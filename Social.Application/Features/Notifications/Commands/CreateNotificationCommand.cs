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
            if (request.Dto == null)
                throw new ArgumentException("Notification payload is required.");
            if (string.IsNullOrWhiteSpace(request.Dto.RecipientId))
                throw new ArgumentException("RecipientId is required.");
            if (string.IsNullOrWhiteSpace(request.Dto.Type))
                throw new ArgumentException("Type is required.");

            var entity = new Notification
            {
                Id = Guid.NewGuid().ToString(),
                UserId = request.Dto.UserId,
                RecipientId = request.Dto.RecipientId,
                Type = request.Dto.Type,
                Message = request.Dto.Message,
                PostId = request.Dto.PostId,
                CommentId = request.Dto.CommentId,
                FollowerId = request.Dto.FollowerId,
                LikeId = request.Dto.LikeId,
                ImageUrl = request.Dto.ImageUrl,
                LastActorName = request.Dto.LastActorName,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(entity, cancellationToken);

            return new NotificationDto
            {
                Id = entity.Id,
                UserId = entity.UserId,
                RecipientId = entity.RecipientId,
                Type = entity.Type,
                Message = entity.Message,
                PostId = entity.PostId,
                CommentId = entity.CommentId,
                FollowerId = entity.FollowerId,
                LikeId = entity.LikeId,
                ImageUrl = entity.ImageUrl,
                IsRead = entity.IsRead,
                IsDeferred = entity.IsDeferred,
                GroupKey = entity.GroupKey,
                ActorCount = entity.ActorCount,
                LastActorName = entity.LastActorName,
                Priority = entity.Priority,
                CreatedAt = entity.CreatedAt
            };
        }
    }

}
