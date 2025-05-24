using AutoMapper;
using MediatR;
using Social.Application.Features.Comments.DTOs;
using Social.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Comments.Queries
{
    public record GetReplyCommentsByParentCommentIdQuery(string parentId) : IRequest<IEnumerable<CommentDto>>;

    public class GetReplyCommentsByParentCommentIdQueryHandler : IRequestHandler<GetReplyCommentsByParentCommentIdQuery, IEnumerable<CommentDto>>
    {
        private readonly ICommentRepository _commentRepository;
        private readonly IMapper _mapper;

        public GetReplyCommentsByParentCommentIdQueryHandler(ICommentRepository commentRepository, IMapper mapper)
        {
            _commentRepository = commentRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CommentDto>> Handle(GetReplyCommentsByParentCommentIdQuery request, CancellationToken cancellationToken)
        {
            var comments = await _commentRepository.GetReplyCommentsByParentCommentIdAsync(request.parentId);
            if (comments == null || !comments.Any())
            {
                return Enumerable.Empty<CommentDto>();
            }

            var commentDtos = _mapper.Map<IEnumerable<CommentDto>>(comments);
            return commentDtos;
        }
    }
}
