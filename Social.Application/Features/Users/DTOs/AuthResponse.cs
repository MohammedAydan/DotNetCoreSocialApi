
namespace Social.Application.Features.Users.DTOs
{
    public class AuthResponse
    {
        public AuthResponse() { }

        public AuthResponse(
            string? message = null,
            List<string>? errors = null,
            UserDto? user = null,
            string? token = null,
            string? type = null,
            string? accessToken = null,
            string? refreshToken = null
        )
        {
            Message = message;
            Errors = errors;
            User = user;
            Token = token;
            Type = type;
            AccessToken = accessToken;
            RefreshToken = refreshToken;
        }

        public string? Message { get; set; }
        public List<string>? Errors { get; set; }
        public UserDto? User { get; set; }
        public string? Token { get; set; }
        public string? Type { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public bool IsSuccess => (Errors == null || !Errors.Any());

        public static AuthResponse Create(
                        string? message = null,
            List<string>? errors = null,
            UserDto? user = null,
            string? token = null,
            string? type = null,
            string? accessToken = null,
            string? refreshToken = null
            )
        {
            return new AuthResponse(
                message: message,
                errors: errors,
                user: user,
                token: token,
                type: type,
                accessToken: accessToken,
                refreshToken: refreshToken
            );
        }
        public bool IsAdmin() => User?.Roles?.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase)) ?? false;
    }
}
