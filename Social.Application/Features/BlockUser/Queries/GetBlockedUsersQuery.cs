using AutoMapper;
using MediatR;
using Social.Application.Features.BlockUser.DTOs;
using Social.Core.Interfaces;

namespace Social.Application.Features.BlockUser.Queries
{
    public record GetBlockedUsersQuery(string UserId, int Page = 1, int Limit = 20) : IRequest<IEnumerable<BlockUserDto>>;

    public class GetBlockedUsersQueryHandler : IRequestHandler<GetBlockedUsersQuery, IEnumerable<BlockUserDto>>
    {
        private readonly IBlockUserRepository _blockUserRepository;
        private readonly IMapper _mapper;

        public GetBlockedUsersQueryHandler(IBlockUserRepository blockUserRepository, IMapper mapper)
        {
            _blockUserRepository = blockUserRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<BlockUserDto>> Handle(GetBlockedUsersQuery request, CancellationToken cancellationToken)
        {
            var blockedUsers = await _blockUserRepository.GetBlockedUsersAsync(request.UserId, request.Page, request.Limit);
            var blockedUserDtos = _mapper.Map<IEnumerable<BlockUserDto>>(blockedUsers);
            return blockedUserDtos;
        }
    }
}