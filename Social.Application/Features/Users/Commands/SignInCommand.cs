using AutoMapper;
using MediatR;
using Social.Application.Features.Users.DTOs;
using Social.Core.Interfaces;
using Social.Core.Entities;


namespace Social.Application.Features.Users.Commends
{
    public record SignInCommand(SignIn SignIn) : IRequest<AuthResponse>;

    public class SignInCommendHandler : IRequestHandler<SignInCommand, AuthResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly Infrastucture.Token.ITokenService _tokenService;

        public SignInCommendHandler(IUserRepository userRepository, IMapper mapper, Infrastucture.Token.ITokenService tokenService)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        }

        public async Task<AuthResponse> Handle(SignInCommand request, CancellationToken cancellationToken)
        {
            string? message = null;
            UserDto? userDto = null;
            string? accessToken = null;
            List<string> errors = new();

            try
            {
                if (request.SignIn == null)
                {
                    message = "SignIn cannot be null";
                    errors.Add(message);
                    return AuthResponse.Create(
                        message: message,
                        errors: errors
                    );
                }

                var user = await _userRepository.SignInAsync(request.SignIn);
                if (user == null)
                {
                    message = "User sign-in failed";
                    errors.Add(message);
                    return AuthResponse.Create(
                        message: message,
                        errors: errors
                    );
                }

                var rolesEnumerable = await _userRepository.GetUserRolesAsync(user);
                var roles = rolesEnumerable?.ToList() ?? new List<string>();
                accessToken = _tokenService.GenerateToken(user, roles);

                userDto = _mapper.Map<UserDto>(user);
                userDto.Roles = roles;

                message = "User sign-in successful";

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
                    message: message ?? "An error occurred during sign-in.",
                    user: userDto,
                    accessToken: accessToken,
                    errors: errors
                );
            }
        }
    }
}
