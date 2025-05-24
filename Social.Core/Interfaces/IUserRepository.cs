using Social.Core.Entities;

namespace Social.Core.Interfaces
{
    public interface IUserRepository
    {
        Task<User> CreateAsync(User user, string password);
        Task<User> SignInAsync(SignIn signIn);

        Task<User> GetUserByIdAsync(string userId, string? myUserId= null);

        Task<User> UpdateUserAsync(User user);

        Task<bool> DeleteUserAsync(string userId);

        Task<IEnumerable<string>> GetUserRolesAsync(User user);

        Task<(IEnumerable<User> Results, int TotalCount)> SearchUsers(
            string q,
            string? currentUserId = null,
            int page = 1,
            int limit = 20);
    }
}
