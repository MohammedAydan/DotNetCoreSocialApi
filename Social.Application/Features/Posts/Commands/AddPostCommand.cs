using MediatR;
using Social.Application.Features.Posts.DTOs;
using Social.Core.Interfaces;
using AutoMapper;
using Social.Core.Entities;

namespace Social.Application.Features.Posts.Commands
{
    public record AddPostCommand(CreatePostRequest createPost, string userId) : IRequest<PostDto>;

    public class AddPostCommandHandler : IRequestHandler<AddPostCommand, PostDto>
    {
        private readonly IPostRepository _postRepository;
        private readonly IMapper _mapper;

        public AddPostCommandHandler(IPostRepository postRepository, IMapper mapper)
        {
            _postRepository = postRepository;
            _mapper = mapper;
        }

        public async Task<PostDto> Handle(AddPostCommand request, CancellationToken cancellationToken)
        {
            var post = _mapper.Map<Post>(request.createPost);
            post.UserId = request.userId;
            var addedPost = await _postRepository.AddPostAsync(post);
            var postDto = _mapper.Map<PostDto>(addedPost);
            return postDto;
        }
    }
}
