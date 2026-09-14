using Social.Core.Entities;

namespace Social.Core.Interfaces
{
    public interface IUserRepository
    {
        Task<User> CreateAsync(User user, string password, CancellationToken cancellationToken = default);
        Task<User> SignInAsync(SignIn signIn, CancellationToken cancellationToken = default);
        Task<bool> ChangePassword(string userId, string currentPassword, string newPassword, string confirmPassword, CancellationToken cancellationToken = default);

        Task<User> GetUserByIdAsync(string userId, string? myUserId = null, CancellationToken cancellationToken = default);

        Task<User> UpdateUserAsync(User user, CancellationToken cancellationToken = default);

        Task<bool> DeleteUserAsync(string userId, CancellationToken cancellationToken = default);

        Task<IEnumerable<string>> GetUserRolesAsync(User user, CancellationToken cancellationToken = default);

        Task<(IEnumerable<User> Results, int TotalCount)> SearchUsers(
            string q,
            string? currentUserId = null,
            int page = 1,
            int limit = 20,
            CancellationToken cancellationToken = default);

        Task<string> CreateRefreshTokenAsync(string userId, CancellationToken cancellationToken = default);
        Task<RefreshToken?> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

        Task<string?> GeneratePasswordResetUrlAsync(string email, CancellationToken cancellationToken = default);
        Task<bool> ResetPasswordAsync(string email, string password, string token, CancellationToken cancellationToken = default);
    }
}
