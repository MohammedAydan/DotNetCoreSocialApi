using AutoMapper;
using MediatR;
using Social.Application.Features.Users.DTOs;
using Social.Core.Interfaces;
using Social.Core.Entities;
using Social.Core;

namespace Social.Application.Features.Users.Commands
{
    public record CreateUserCommand(CreateUserRequest CreateUserRequest) : IRequest<AuthResponse>;

    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, AuthResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly Infrastucture.Token.ITokenService _tokenService;

        public CreateUserCommandHandler(IUserRepository userRepository, IMapper mapper, Infrastucture.Token.ITokenService tokenService)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        }

        public async Task<AuthResponse> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            if (request.CreateUserRequest == null)
                return AuthResponse.Create("CreateUserRequest cannot be null", errors: new List<string> { "Invalid request" });

            try
            {
                var user = _mapper.Map<User>(request.CreateUserRequest);

                var result = await _userRepository.CreateAsync(user, request.CreateUserRequest.Password);
                if (result == null)
                    return AuthResponse.Create("User creation failed", errors: new List<string> { "Unable to create user" });

                var roles = (await _userRepository.GetUserRolesAsync(result))?.ToList() ?? new List<string>();

                var accessToken = _tokenService.GenerateToken(result, roles);
                var refreshToken = await _userRepository.CreateRefreshTokenAsync(result.Id);

                var userDto = _mapper.Map<UserDto>(result);
                userDto.Roles = roles;

                var userGender = result.UserGender.ToLower() == UserGenderTypes.Female
                    ? UserGenderTypes.Female
                    : UserGenderTypes.Male;

                userDto.UserGender = userGender;

                return AuthResponse.Create(
                    message: "User created successfully",
                    user: userDto,
                    accessToken: accessToken,
                    refreshToken: refreshToken
                );
            }
            catch (Exception)
            {
                // log exception internally
                return AuthResponse.Create(
                    message: "An error occurred during user creation.",
                    errors: new List<string> { "Unexpected error" }
                );
            }
        }
    }

}
