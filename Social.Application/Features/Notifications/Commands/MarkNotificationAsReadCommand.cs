using MediatR;
using Social.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Notifications.Commands
{
    public record MarkNotificationAsReadCommand(string Id) : IRequest;

    public class MarkNotificationAsReadCommandHandler : IRequestHandler<MarkNotificationAsReadCommand>
    {
        private readonly INotificationRepository _repo;

        public MarkNotificationAsReadCommandHandler(INotificationRepository repo)
        {
            _repo = repo;
        }

        public async Task Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
        {
            var notification = await _repo.GetByIdAsync(request.Id);

            if (notification == null)
                throw new KeyNotFoundException("Notification not found.");

            notification.IsRead = true;

            await _repo.UpdateAsync(notification);
        }
    }
}
