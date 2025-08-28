using Social.Core.Entities;

namespace Social.Core.Interfaces
{
    public interface IUserRepository
    {
        Task<User> CreateAsync(User user, string password);
        Task<User> SignInAsync(SignIn signIn);
        Task<bool> ChangePassword(string userId, string currentPassword, string newPassword, string confirmPassword);

        Task<User> GetUserByIdAsync(string userId, string? myUserId= null);

        Task<User> UpdateUserAsync(User user);

        Task<bool> DeleteUserAsync(string userId);

        Task<IEnumerable<string>> GetUserRolesAsync(User user);

        Task<(IEnumerable<User> Results, int TotalCount)> SearchUsers(
            string q,
            string? currentUserId = null,
            int page = 1,
            int limit = 20);

        Task<string> CreateRefreshTokenAsync(string userId);
        Task<RefreshToken?> ValidateRefreshTokenAsync(string refreshToken);

        Task<string?> GeneratePasswordResetUrlAsync(string email);
        Task<bool> ResetPasswordAsync(string email, string password, string token);
    }
}
