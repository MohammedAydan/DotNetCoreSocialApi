using AutoMapper;
using MediatR;
using Social.Application.Features.Users.DTOs;
using Social.Core.Interfaces;
using Social.Core.Entities;


namespace Social.Application.Features.Users.Commends
{
    public record SignInCommand(SignIn SignIn) : IRequest<AuthResponse>;

    public class SignInCommandHandler : IRequestHandler<SignInCommand, AuthResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly Infrastucture.Token.ITokenService _tokenService;

        public SignInCommandHandler(IUserRepository userRepository, IMapper mapper, Infrastucture.Token.ITokenService tokenService)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        }

        public async Task<AuthResponse> Handle(SignInCommand request, CancellationToken cancellationToken)
        {
            var errors = new List<string>();

            try
            {
                if (request.SignIn == null)
                    return AuthResponse.Create(message: "SignIn cannot be null", errors: new List<string> { "SignIn cannot be null" });

                var user = await _userRepository.SignInAsync(request.SignIn);
                if (user == null)
                    return AuthResponse.Create(message: "User sign-in failed", errors: new List<string> { "Invalid credentials" });

                var roles = (await _userRepository.GetUserRolesAsync(user))?.ToList() ?? new List<string>();

                var accessToken = _tokenService.GenerateToken(user, roles);
                var refreshToken = await _userRepository.CreateRefreshTokenAsync(user.Id);

                var userDto = _mapper.Map<UserDto>(user);
                userDto.Roles = roles;

                return AuthResponse.Create(
                    message: "User sign-in successful",
                    user: userDto,
                    accessToken: accessToken,
                    refreshToken: refreshToken,
                    errors: errors
                );
            }
            catch (Exception)
            {
                // log exception internally
                return AuthResponse.Create(
                    message: "An error occurred during sign-in.",
                    errors: new List<string> { "Unexpected error" }
                );
            }
        }
    }
}
