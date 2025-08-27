using AutoMapper;
using MediatR;
using Social.Application.Features.Users.DTOs;
using Social.Core.Interfaces;
using Social.Infrastucture.Token;

namespace Social.Application.Features.Users.Commands
{
    public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponse>;

    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;

        public RefreshTokenCommandHandler(
            IUserRepository userRepository,
            ITokenService tokenService,
            IMapper mapper)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var errors = new List<string>();

            try
            {
                if (string.IsNullOrWhiteSpace(request.RefreshToken))
                {
                    errors.Add("Refresh token is required");
                    return AuthResponse.Create(message: "Invalid request", errors: errors);
                }

                var existingToken = await _userRepository.ValidateRefreshTokenAsync(request.RefreshToken);
                if (existingToken == null || existingToken.IsExpired)
                {
                    errors.Add("Invalid or expired refresh token");
                    return AuthResponse.Create(message: "Invalid or expired refresh token", errors: errors);
                }

                var user = await _userRepository.GetUserByIdAsync(existingToken.UserId);
                if (user == null)
                {
                    errors.Add("User not found");
                    return AuthResponse.Create(message: "User not found", errors: errors);
                }

                var roles = (await _userRepository.GetUserRolesAsync(user))?.ToList() ?? new List<string>();

                var accessToken = _tokenService.GenerateToken(user, roles);

                var newRefreshToken = await _userRepository.CreateRefreshTokenAsync(user.Id);

                var userDto = _mapper.Map<UserDto>(user);
                userDto.Roles = roles;

                return AuthResponse.Create(
                    message: "Token refreshed successfully",
                    user: userDto,
                    accessToken: accessToken,
                    refreshToken: newRefreshToken,
                    errors: errors
                );
            }
            catch (Exception ex)
            {
                errors.Add("Unexpected error");
                return AuthResponse.Create(
                    message: "An error occurred while refreshing the token.",
                    errors: errors
                );
            }
        }
    }
}
