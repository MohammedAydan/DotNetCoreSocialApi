using AutoMapper;
using MediatR;
using Social.Application.Features.Comments.DTOs;
using Social.Core.Entities;
using Social.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Comments.Commands
{
    public record AddCommentCommand(CreateCommentRequest CommentRequest, string UserId) : IRequest<CommentDto>;

    public class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, CommentDto>
    {
        private readonly ICommentRepository _commentRepository;
        private readonly IMapper _mapper;

        public AddCommentCommandHandler(ICommentRepository commentRepository, IMapper mapper)
        {
            _commentRepository = commentRepository;
            _mapper = mapper;
        }

        public async Task<CommentDto> Handle(AddCommentCommand request, CancellationToken cancellationToken)
        {
            var comment = _mapper.Map<Comment>(request.CommentRequest);
            comment.UserId = request.UserId;

            var addedComment = await _commentRepository.AddCommentAsync(comment);
            if (addedComment == null)
            {
                throw new Exception("Failed to add comment");
            }
            
            var commentDto = _mapper.Map<CommentDto>(addedComment);

            return commentDto;
        }
    }
}
