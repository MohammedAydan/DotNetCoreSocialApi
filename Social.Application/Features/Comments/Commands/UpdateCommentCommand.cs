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
    public record UpdateCommentCommand(UpdateCommentRequest UpdateComment, string UserId) : IRequest<CommentDto>;

    public class UpdateCommentCommandHandler : IRequestHandler<UpdateCommentCommand, CommentDto>
    {
        private readonly ICommentRepository _commentRepository;
        private readonly IMapper _mapper;

        public UpdateCommentCommandHandler(ICommentRepository commentRepository, IMapper mapper)
        {
            _commentRepository = commentRepository;
            _mapper = mapper;
        }

        public async Task<CommentDto> Handle(UpdateCommentCommand request, CancellationToken cancellationToken)
        {
            var comment = _mapper.Map<Comment>(request.UpdateComment);
            var updatedComment = await _commentRepository.UpdateCommentAsync(comment, request.UserId);
            if (updatedComment == null)
            {
                throw new Exception("Failed to update comment.");
            }

            var commentDto = _mapper.Map<CommentDto>(updatedComment);
            return commentDto;
        }
    }
}
