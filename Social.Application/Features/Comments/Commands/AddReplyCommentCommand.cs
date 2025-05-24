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
    public record AddReplyCommentCommand(CreateReplyCommentRequest createReplyComment, string UserId) : IRequest<CommentDto>;

    public class AddReplyCommentCommandHandler : IRequestHandler<AddReplyCommentCommand, CommentDto>
    {
        private readonly ICommentRepository _commentRepository;
        private readonly IMapper _mapper;

        public AddReplyCommentCommandHandler(ICommentRepository commentRepository, IMapper mapper)
        {
            _commentRepository = commentRepository;
            _mapper = mapper;
        }

        public async Task<CommentDto> Handle(AddReplyCommentCommand request, CancellationToken cancellationToken)
        {
            var comment = _mapper.Map<Comment>(request.createReplyComment);
            comment.UserId = request.UserId;

            var addedComment = await _commentRepository.AddReplyAsync(comment);
            if (addedComment == null)
            {
                throw new Exception("Failed to add comment");
            }

            var commentDto = _mapper.Map<CommentDto>(addedComment);

            return commentDto;
        }
    }
}
