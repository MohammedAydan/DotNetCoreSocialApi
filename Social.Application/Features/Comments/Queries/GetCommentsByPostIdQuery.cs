using AutoMapper;
using MediatR;
using Social.Application.Features.Comments.DTOs;
using Social.Core.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Comments.Queries
{
    public record GetCommentsByPostIdQuery(string postId, int page = 1, int limit = 10) : IRequest<IEnumerable<CommentDto>>;

    public class GetCommentsByPostIdQueryHandler : IRequestHandler<GetCommentsByPostIdQuery, IEnumerable<CommentDto>>
    {
        private readonly ICommentRepository _commentRepository;
        private readonly IMapper _mapper;

        public GetCommentsByPostIdQueryHandler(ICommentRepository commentRepository, IMapper mapper)
        {
            _commentRepository = commentRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CommentDto>> Handle(GetCommentsByPostIdQuery request, CancellationToken cancellationToken)
        {
            var comments = await _commentRepository.GetCommentsByPostIdAsync(request.postId, request.page, request.limit);
            if (comments == null || !comments.Any())
            {
                return Enumerable.Empty<CommentDto>();
            }

            var commentDtos = _mapper.Map<IEnumerable<CommentDto>>(comments);
            return commentDtos;
        }
    }
}
