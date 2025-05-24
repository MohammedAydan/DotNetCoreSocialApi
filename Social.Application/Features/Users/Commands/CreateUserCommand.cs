using AutoMapper;
using MediatR;
using Social.Application.Features.Users.DTOs;
using Social.Core.Interfaces;
using Social.Core.Entities;

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
            string? message = null;
            UserDto? userDto = null;
            string? accessToken = null;
            List<string> errors = new();

            try
            {
                if (request.CreateUserRequest == null)
                {
                    message = "CreateUserRequest cannot be null";
                    errors.Add(message);
                    return AuthResponse.Create(
                        message: message,
                        errors: errors
                    );
                }

                var user = _mapper.Map<User>(request.CreateUserRequest);

                var result = await _userRepository.CreateAsync(user, request.CreateUserRequest.Password);
                if (result == null)
                {
                    message = "User creation failed";
                    errors.Add(message);
                    return AuthResponse.Create(
                        message: message,
                        errors: errors
                    );
                }

                var rolesEnumerable = await _userRepository.GetUserRolesAsync(result);
                var roles = rolesEnumerable?.ToList() ?? new List<string>();
                accessToken = _tokenService.GenerateToken(result, roles);

                userDto = _mapper.Map<UserDto>(result);
                userDto.Roles = roles;

                message = "User created successfully";

                return AuthResponse.Create(
                    message: message,
                    user: userDto,
                    accessToken: accessToken,
                    errors: errors
                );
            }
            catch (Exception ex)
            {
                errors.Add(ex.Message);
                return AuthResponse.Create(
                    message: message ?? "An error occurred during user creation.",
                    user: userDto,
                    accessToken: accessToken,
                    errors: errors
                );
            }
        }
    }
}
