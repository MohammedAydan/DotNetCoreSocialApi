using AutoMapper;
using MediatR;
using Social.Application.Features.Like.DTOs;
using Social.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Like.Queries
{
    public record GetLikesByPostIdQuery(string postId, int page = 1, int limit = 20) : IRequest<IEnumerable<LikeDto>>;

    public class GetLikesByPostIdQueryHandler : IRequestHandler<GetLikesByPostIdQuery, IEnumerable<LikeDto>>
    {
        private readonly ILikeRepository _likeRepository;
        private readonly IMapper _mapper;

        public GetLikesByPostIdQueryHandler(ILikeRepository likeRepository, IMapper mapper)
        {
            _likeRepository = likeRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<LikeDto>> Handle(GetLikesByPostIdQuery request, CancellationToken cancellationToken)
        {
            var likes = await _likeRepository.GetLikesByPostIdAsync(request.postId, request.page, request.limit);

            return _mapper.Map<IEnumerable<LikeDto>>(likes);
        }
    }
}
