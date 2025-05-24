using AutoMapper;
using MediatR;
using Social.Application.Features.Users.DTOs;
using Social.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Users.Queries
{
    public record SearchUsersQuery(string q, string? currentUserId = null, int page = 1, int limit = 20) : IRequest<(IEnumerable<UserDto> Results, int TotalCount)>;

    public class SearchUsersQueryHandler : IRequestHandler<SearchUsersQuery, (IEnumerable<UserDto> Results, int TotalCount)>
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public SearchUsersQueryHandler(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<(IEnumerable<UserDto> Results, int TotalCount)> Handle(SearchUsersQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.q))
            {
                return (Enumerable.Empty<UserDto>(), 0);
            }

            var (users, totalCount) = await _userRepository.SearchUsers(request.q, request.currentUserId, request.page, request.limit);

            var userDtos = _mapper.Map<IEnumerable<UserDto>>(users);

            return (userDtos, totalCount);
        }
    }
}
