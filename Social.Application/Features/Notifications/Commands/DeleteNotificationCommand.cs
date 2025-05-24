using MediatR;
using Social.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Notifications.Commands
{
    public record DeleteNotificationCommand(string Id) : IRequest;

    public class DeleteNotificationCommandHandler : IRequestHandler<DeleteNotificationCommand>
    {
        private readonly INotificationRepository _repo;

        public DeleteNotificationCommandHandler(INotificationRepository repo)
        {
            _repo = repo;
        }

        public async Task Handle(DeleteNotificationCommand request, CancellationToken cancellationToken)
        {
            var notification = await _repo.GetByIdAsync(request.Id);

            if (notification == null)
                throw new KeyNotFoundException("Notification not found.");

            await _repo.DeleteAsync(request.Id);
        }
    }
}
