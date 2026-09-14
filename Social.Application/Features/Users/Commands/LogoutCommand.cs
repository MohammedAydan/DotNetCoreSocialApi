using MediatR;
using Microsoft.Extensions.Configuration;
using Social.Core.Common;
using Social.Core.Interfaces;

namespace Social.Application.Features.Users.Commands
{
    public record LogoutCommand(string AccessToken) : IRequest<ApiResponse<bool>>;

    public class LogoutCommandHandler : IRequestHandler<LogoutCommand, ApiResponse<bool>>
    {
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _configuration;

        public LogoutCommandHandler(ITokenService tokenService, IConfiguration configuration)
        {
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<ApiResponse<bool>> Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.AccessToken))
            {
                return ApiResponse<bool>.ErrorResponse("Access token is required");
            }

            // Blacklist the token for the same duration as JWT expiration
            var jwtExpireTimeMinutes = int.TryParse(_configuration["Jwt:ExpireTime"], out var minutes) ? minutes : 60;
            var tokenExpiration = TimeSpan.FromMinutes(jwtExpireTimeMinutes);

            await _tokenService.BlacklistTokenAsync(request.AccessToken, tokenExpiration);

            return ApiResponse<bool>.SuccessResponse("Logged out successfully", true);
        }
    }
}