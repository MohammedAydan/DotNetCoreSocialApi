using AutoMapper;
using MediatR;
using MySqlX.XDevAPI.Common;
using Social.Application.Features.Users.DTOs;
using Social.Core;
using Social.Core.Entities;
using Social.Core.Interfaces;

namespace Social.Application.Features.Users.Commends
{
    public record UpdateUserCommand(UpdateUserDto UserDto) : IRequest<UserDto>;

    public class UpdateUserCommedHandler : IRequestHandler<UpdateUserCommand, UserDto>
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public UpdateUserCommedHandler(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _mapper = mapper;
        }
        public async Task<UserDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            if (request.UserDto == null)
            {
                throw new ArgumentNullException(nameof(request.UserDto), "UserDto cannot be null");
            }

            User? user = _mapper.Map<User>(request.UserDto);
            var userGender = string.Equals(user.UserGender, UserGenderTypes.Male, StringComparison.OrdinalIgnoreCase)
                ? UserGenderTypes.Male
                : UserGenderTypes.Female;
            user.UserGender = userGender;


            User? userResult = await _userRepository.UpdateUserAsync(user);
            if (userResult == null)
            {
                throw new InvalidOperationException("User update failed");
            }

            UserDto? userDtoResult = _mapper.Map<UserDto>(userResult);

            if (userDtoResult == null) {
                throw new InvalidOperationException("Mapping to UserDto failed");
            }

            return userDtoResult;
        }
    }
}
