using MediatR;
using Social.Application.Features.Like.DTOs;
using Social.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Like.Commands
{
    public record AddOrRemoveLikeCommand(LikeRequest likeRequest, string UserId) : IRequest<bool>;

    public class AddOrRemoveLikeCommandHandler : IRequestHandler<AddOrRemoveLikeCommand, bool>
    {
        private readonly ILikeRepository _likeRepository;
        public AddOrRemoveLikeCommandHandler(ILikeRepository likeRepository)
        {
            _likeRepository = likeRepository;
        }

        public async Task<bool> Handle(AddOrRemoveLikeCommand request, CancellationToken cancellationToken)
        {
            return await _likeRepository.AddOrRemoveLikeAsync(request.likeRequest.PostId, request.UserId);
        }
    }
}
