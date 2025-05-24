using MediatR;
using Social.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Notifications.Commands
{
    public record DeleteAllNotificationsForUserCommand(string UserId) : IRequest;

    public class DeleteAllNotificationsForUserCommandHandler : IRequestHandler<DeleteAllNotificationsForUserCommand>
    {
        private readonly INotificationRepository _repo;

        public DeleteAllNotificationsForUserCommandHandler(INotificationRepository repo)
        {
            _repo = repo;
        }

        public async Task Handle(DeleteAllNotificationsForUserCommand request, CancellationToken cancellationToken)
        {
            var notifications = await _repo.GetByUserIdAsync(request.UserId);

            foreach (var notification in notifications)
            {
                await _repo.DeleteAsync(notification.Id);
            }
        }
    }
}
