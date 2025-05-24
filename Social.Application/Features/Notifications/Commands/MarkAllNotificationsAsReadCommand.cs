using MediatR;
using Social.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Notifications.Commands
{
    public record MarkAllNotificationsAsReadCommand(string UserId) : IRequest;

    public class MarkAllNotificationsAsReadCommandHandler : IRequestHandler<MarkAllNotificationsAsReadCommand>
    {
        private readonly INotificationRepository _repo;

        public MarkAllNotificationsAsReadCommandHandler(INotificationRepository repo)
        {
            _repo = repo;
        }

        public async Task Handle(MarkAllNotificationsAsReadCommand request, CancellationToken cancellationToken)
        {
            var notifications = await _repo.GetUnreadByUserIdAsync(request.UserId);

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                await _repo.UpdateAsync(notification);
            }
        }
    }
}
