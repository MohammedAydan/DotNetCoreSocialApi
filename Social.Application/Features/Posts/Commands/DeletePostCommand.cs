using MediatR;
using Social.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Posts.Commands
{
    public record DeletePostCommand(string postId, string userId) : IRequest<bool>;

    public class DeletePostCommandHandler(IPostRepository _postRepository) : IRequestHandler<DeletePostCommand, bool>
    {
        public async Task<bool> Handle(DeletePostCommand request, CancellationToken cancellationToken)
        {
            return await _postRepository.DeletePostAsync(request.postId, request.userId);
        }
    }
}
