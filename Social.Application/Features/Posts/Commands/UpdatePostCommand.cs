using AutoMapper;
using MediatR;
using Social.Application.Features.Posts.DTOs;
using Social.Core.Entities;
using Social.Core.Interfaces;

namespace Social.Application.Features.Posts.Commands
{
    public record UpdatePostCommand(UpdatePostRequest updatePost, string userId) : IRequest<PostDto>;

    public class UpdatePostCommandHandler : IRequestHandler<UpdatePostCommand, PostDto>
    {
        private readonly IPostRepository _postRepository;
        private readonly IMapper _mapper;

        public UpdatePostCommandHandler(IPostRepository postRepository, IMapper mapper)
        {
            _postRepository = postRepository;
            _mapper = mapper;
        }

        public async Task<PostDto> Handle(UpdatePostCommand request, CancellationToken cancellationToken)
        {
            var post = _mapper.Map<Post>(request.updatePost);
            post.UserId = request.userId;
            var addedPost = await _postRepository.UpdatePostAsync(post);
            var postDto = _mapper.Map<PostDto>(addedPost);
            return postDto;
        }
    }
}
