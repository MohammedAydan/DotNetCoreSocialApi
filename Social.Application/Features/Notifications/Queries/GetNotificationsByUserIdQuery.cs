using AutoMapper;
using MediatR;
using Social.Application.Features.Notifications.DTOs;
using Social.Application.Features.Users.DTOs;
using Social.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Notifications.Queries
{
    public record GetNotificationsByUserIdQuery(string UserId, int Page, int Limit) : IRequest<IEnumerable<NotificationDto>>;

    public class GetNotificationsByUserIdQueryHandler : IRequestHandler<GetNotificationsByUserIdQuery, IEnumerable<NotificationDto>>
    {
        private readonly INotificationRepository _repo;
        private readonly IMapper _mapper;

        public GetNotificationsByUserIdQueryHandler(INotificationRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<IEnumerable<NotificationDto>> Handle(GetNotificationsByUserIdQuery request, CancellationToken cancellationToken)
        {
            var notifications = await _repo.GetByUserIdAsync(request.UserId, request.Page, request.Limit);

            return notifications.Select(notification => new NotificationDto
            {
                Id = notification.Id,
                UserId = notification.UserId,
                SenderUser = _mapper.Map<UserDto>(notification.SenderUser),
                //RecipientUser = _mapper.Map<UserDto>(notification.RecipientUser),
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
