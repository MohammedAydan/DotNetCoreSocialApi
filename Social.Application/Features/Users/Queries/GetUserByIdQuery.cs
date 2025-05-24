using Social.Application.Features.Users.DTOs;
using MediatR;
using Social.Core.Interfaces;
using Social.Core.Entities;
using AutoMapper;


namespace Social.Application.Features.Users.Queries
{
    public record GetUserByIdQuery(string Id, string? myUserId = null) : IRequest<UserDto>;

    public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public GetUserByIdQueryHandler(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.Id))
            {
                throw new ArgumentNullException(nameof(request.Id), "User ID cannot be null or empty");
            }
            var user = await _userRepository.GetUserByIdAsync(request.Id, request.myUserId);
            return _mapper.Map<UserDto>(user);
        }
    }
}
