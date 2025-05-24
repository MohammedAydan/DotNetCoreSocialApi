using MediatR;
using Social.Application.Features.Posts.DTOs;
using Social.Core.Interfaces;
using AutoMapper;

namespace Social.Application.Features.Posts.Queries
{
    public record GetMyPostsQuery(string UserId, int Page = 1, int Limit = 20) : IRequest<IEnumerable<PostDto>>;

    public class GetMyPostsQuesryHandler : IRequestHandler<GetMyPostsQuery, IEnumerable<PostDto>>
    {
        private readonly IPostRepository _postRepository;
        private readonly IMapper _mapper;

        public GetMyPostsQuesryHandler(IPostRepository postRepository, IMapper mapper)
        {
            _postRepository = postRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<PostDto>> Handle(GetMyPostsQuery request, CancellationToken cancellationToken)
        {
            var posts = await _postRepository.GetMyPostsAsync(request.UserId, request.Page, request.Limit);
            if (posts == null || !posts.Any())
                return Enumerable.Empty<PostDto>();

            var postsDto = _mapper.Map<IEnumerable<PostDto>>(posts);

            return postsDto;
        }
    }
}
