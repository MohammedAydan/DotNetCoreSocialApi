using AutoMapper;
using MediatR;
using Social.Application.Features.Posts.DTOs;
using Social.Core.Entities;
using Social.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Posts.Commands
{
    public record SharePostCommand(SharePostRequest sharePost, string userId) : IRequest<PostDto>;

    public class SharePostCommandHandler : IRequestHandler<SharePostCommand, PostDto>
    {
        private readonly IPostRepository _postRepository;
        private readonly IMapper _mapper;

        public SharePostCommandHandler(IPostRepository postRepository, IMapper mapper)
        {
            _postRepository = postRepository;
            _mapper = mapper;
        }

        public async Task<PostDto> Handle(SharePostCommand request, CancellationToken cancellationToken)
        {
            var post = _mapper.Map<Post>(request.sharePost);
            post.UserId = request.userId;
            var addedPost = await _postRepository.SharePostAsync(request.sharePost.ParentPostId, post);
            var postDto = _mapper.Map<PostDto>(addedPost);
            return postDto;
        }
    }
}
