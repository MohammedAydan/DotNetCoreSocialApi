using AutoMapper;
using MediatR;
using Social.Application.Features.Posts.DTOs;
using Social.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Posts.Queries
{
    public record GetFeedPostsQuery(string UserId, int Page = 1, int Limit = 20) : IRequest<IEnumerable<PostDto>>;

    public class GetFeedPostsQueryHandler : IRequestHandler<GetFeedPostsQuery, IEnumerable<PostDto>>
    {
        private readonly IPostRepository _postRepository;
        private readonly IMapper _mapper;

        public GetFeedPostsQueryHandler(IPostRepository postRepository, IMapper mapper)
        {
            _postRepository = postRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<PostDto>> Handle(GetFeedPostsQuery request, CancellationToken cancellationToken)
        {
            var posts = await _postRepository.GetFeedPostsAsync(request.UserId, request.Page, request.Limit);
            var postsDto = _mapper.Map<IEnumerable<PostDto>>(posts);

            return postsDto;
        }
    }
}
