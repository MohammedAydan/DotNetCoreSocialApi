using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Social.Core.Entities;
using Social.Core.Interfaces;
using Social.Infrastucture.Data;
using System;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;

namespace Social.Infrastucture.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly GenralConfig _genralConfig;

        public UserRepository(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IOptions<GenralConfig> genralConfig)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _genralConfig = genralConfig.Value;
        }

        public async Task<User> CreateAsync(User user, string password)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be null or empty.", nameof(password));

            await EnsureRolesExistAsync();

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");

            await _userManager.AddToRoleAsync(user, "User");
            return user;
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Failed to delete user: {string.Join(", ", result.Errors.Select(e => e.Description))}");

            return true;
        }

        public async Task<User> GetUserByIdAsync(string userId, string? myUserId = null)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new InvalidOperationException("User not found.");

            if (myUserId != null && myUserId != userId)
            {
                var follower = await _context.Followers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.FollowerId == myUserId && f.FollowingId == userId);

                user.IsFollower = follower != null;
                user.IsFollowerAccepted = follower?.Accepted ?? false;

                var following = await _context.Followers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.FollowerId == userId && f.FollowingId == myUserId);

                user.IsFollowing = following != null;
                user.IsFollowingAccepted = following?.Accepted ?? false;
            }

            return user;
        }

        public async Task<User> SignInAsync(SignIn signIn)
        {
            if (signIn == null)
                throw new ArgumentNullException(nameof(signIn));

            var user = await _userManager.FindByEmailAsync(signIn.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, signIn.Password))
                throw new AuthenticationException("Invalid email or password.");

            if (await _userManager.IsLockedOutAsync(user))
                throw new AuthenticationException("This account has been locked. Please contact support.");

            await _userManager.ResetAccessFailedCountAsync(user);
            return user;
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            var existingUser = await _userManager.FindByIdAsync(user.Id);
            if (existingUser == null)
                throw new InvalidOperationException("User not found.");

            if (user.FirstName != null && user.FirstName != existingUser.FirstName)
                existingUser.FirstName = user.FirstName;

            if (user.LastName != null && user.LastName != existingUser.LastName)
                existingUser.LastName = user.LastName;

            if (user.UserGender != null && user.UserGender != existingUser.UserGender)
                existingUser.UserGender = user.UserGender;

            if (user.BirthDate != default && user.BirthDate != existingUser.BirthDate)
                existingUser.BirthDate = user.BirthDate;

            if (user.Bio != null && user.Bio != existingUser.Bio)
                existingUser.Bio = user.Bio;

            if (user.ProfileImageUrl != null && user.ProfileImageUrl != existingUser.ProfileImageUrl)
                existingUser.ProfileImageUrl = user.ProfileImageUrl;

            if (user.CoverImageUrl != null && user.CoverImageUrl != existingUser.CoverImageUrl)
                existingUser.CoverImageUrl = user.CoverImageUrl;

            if (user.IsPrivate != existingUser.IsPrivate)
                existingUser.IsPrivate = user.IsPrivate;

            if (user.IsVerified != existingUser.IsVerified)
                existingUser.IsVerified = user.IsVerified;

            var result = await _userManager.UpdateAsync(existingUser);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Failed to update user: {string.Join(", ", result.Errors.Select(e => e.Description))}");

            return existingUser;
        }

        public async Task<IEnumerable<string>> GetUserRolesAsync(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            var roles = await _userManager.GetRolesAsync(user);
            return roles ?? throw new InvalidOperationException("Failed to retrieve user roles.");
        }

        public async Task<(IEnumerable<User> Results, int TotalCount)> SearchUsers(
        string q,
        string? currentUserId = null,
        int page = 1,
        int limit = 20)
        {
            try
            {
                // Sanitize pagination
                page = Math.Max(1, page);
                limit = Math.Clamp(limit, 1, 100);

                // Guard clause for empty query
                if (string.IsNullOrWhiteSpace(q))
                {
                    return (Enumerable.Empty<User>(), 0);
                }

                // Ensure q is not null before normalizing
                var normalizedQuery = q?.Trim().ToUpperInvariant() ?? string.Empty;

                // Ensure context is initialized
                if (_context == null)
                {
                    throw new InvalidOperationException("Database context is not initialized");
                }

                // Ensure Users DbSet is available
                if (_context.Users == null)
                {
                    throw new InvalidOperationException("Users DbSet is not available");
                }

                // Create a base query
                var query = _context.Users.AsNoTracking();

                // Exclude current user if provided
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    query = query.Where(u => u.Id != currentUserId);
                }

                // Instead of complex LINQ expressions that might cause issues in MySQL,
                // fetch a larger set of data and filter in-memory
                var allUsers = await query.ToListAsync().ConfigureAwait(false);

                // Filter in memory
                var filteredUsers = allUsers.Where(u =>
                    (u.NormalizedUserName != null && u.NormalizedUserName.IndexOf(normalizedQuery, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (u.NormalizedEmail != null && u.NormalizedEmail.IndexOf(normalizedQuery, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (u.FirstName != null && u.FirstName.ToUpper().IndexOf(normalizedQuery, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (u.LastName != null && u.LastName.ToUpper().IndexOf(normalizedQuery, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (u.Bio != null && u.Bio.ToUpper().IndexOf(normalizedQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                ).ToList();

                // Total count of filtered users
                var totalCount = filteredUsers.Count;

                // Sort in memory with safe null checks
                var sortedUsers = filteredUsers.OrderByDescending(u =>
                    (u.NormalizedUserName != null && u.NormalizedUserName.Equals(normalizedQuery, StringComparison.OrdinalIgnoreCase)) ? 3 :
                    (u.NormalizedUserName != null && u.NormalizedUserName.StartsWith(normalizedQuery, StringComparison.OrdinalIgnoreCase)) ? 2 :
                    (u.UserName != null && u.UserName.IndexOf(normalizedQuery, StringComparison.OrdinalIgnoreCase) >= 0) ? 1 : 0
                ).ThenBy(u => u.NormalizedUserName ?? string.Empty);

                // Paginate in memory
                var pagedUsers = sortedUsers
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .Select(u => new User
                    {
                        Id = u.Id ?? string.Empty,
                        UserName = u.UserName ?? string.Empty,
                        Email = u.Email ?? string.Empty,
                        FirstName = u.FirstName ?? string.Empty,
                        LastName = u.LastName ?? string.Empty,
                        ProfileImageUrl = u.ProfileImageUrl ?? string.Empty,
                        Bio = u.Bio ?? string.Empty,
                        IsPrivate = u.IsPrivate,
                        IsVerified = u.IsVerified,
                        CreatedAt = u.CreatedAt,
                        FollowersCount = u.FollowersCount,
                        FollowingCount = u.FollowingCount,
                        PostsCount = u.PostsCount
                    })
                    .ToList();

                // Safely handle the case when no users are found
                if (string.IsNullOrEmpty(currentUserId) || !pagedUsers.Any())
                {
                    return (pagedUsers, totalCount);
                }

                // Ensure Followers DbSet is available
                if (_context.Followers == null)
                {
                    return (pagedUsers, totalCount);
                }

                var userIds = pagedUsers.Select(u => u.Id).Where(id => !string.IsNullOrEmpty(id)).ToList();

                // Guard against empty userIds list
                if (!userIds.Any())
                {
                    return (pagedUsers, totalCount);
                }

                // Load follow relationships for these users
                var followRelations = await _context.Followers
                    .AsNoTracking()
                    .Where(f =>
                        (f.FollowerId == currentUserId && userIds.Contains(f.FollowingId)) ||
                        (f.FollowingId == currentUserId && userIds.Contains(f.FollowerId)))
                    .Select(f => new
                    {
                        f.FollowerId,
                        f.FollowingId,
                        f.Accepted
                    })
                    .ToListAsync()
                    .ConfigureAwait(false);

                // Safe null check for properties
                foreach (var user in pagedUsers)
                {
                    // Ensure user.Id is not null before using it in comparisons
                    if (string.IsNullOrEmpty(user.Id))
                    {
                        continue;
                    }

                    var following = followRelations.FirstOrDefault(f =>
                        f.FollowerId == currentUserId && f.FollowingId == user.Id);
                    user.IsFollowing = following != null;
                    user.IsFollowingAccepted = following?.Accepted ?? false;

                    var follower = followRelations.FirstOrDefault(f =>
                        f.FollowerId == user.Id && f.FollowingId == currentUserId);
                    user.IsFollower = follower != null;
                    user.IsFollowerAccepted = follower?.Accepted ?? false;
                }

                return (pagedUsers, totalCount);
            }
            catch (Exception ex)
            {
                // Add stack trace information to help debug the issue
                throw new Exception($"Error searching users: {ex.Message} - Stack trace: {ex.StackTrace}", ex);
            }
        }


        /// <summary>
        /// Ensure default roles like "User" and "Admin" exist in the system.
        /// </summary>
        private async Task EnsureRolesExistAsync()
        {
            if (!await _roleManager.RoleExistsAsync("User"))
            {
                var result = await _roleManager.CreateAsync(new IdentityRole("User"));
                if (!result.Succeeded)
                    throw new InvalidOperationException($"Failed to create 'User' role: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                var result = await _roleManager.CreateAsync(new IdentityRole("Admin"));
                if (!result.Succeeded)
                    throw new InvalidOperationException($"Failed to create 'Admin' role: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        public async Task<string> CreateRefreshTokenAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new InvalidOperationException("User not found.");

            var existingToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.UserId == userId);
            if (existingToken != null)
                _context.RefreshTokens.Remove(existingToken);

            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            var refreshToken = Convert.ToBase64String(randomNumber);

            var refreshTokenEntity = new RefreshToken
            {
                Token = refreshToken,
                UserId = userId,
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow,
            };

            await _context.RefreshTokens.AddAsync(refreshTokenEntity);
            await _context.SaveChangesAsync();

            return refreshToken;
        }

        public async Task<RefreshToken?> ValidateRefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new ArgumentException("Refresh token cannot be null or empty.", nameof(refreshToken));

            var tokenEntity = await _context.RefreshTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (tokenEntity == null || tokenEntity.Expires < DateTime.UtcNow)
            {
                return null; 
            }

            return tokenEntity;
        }

        public async Task<bool> ChangePassword(string userId, string currentPassword, string newPassword, string confirmPassword)
        {
            User? currentUser = await _context.Users.FindAsync(userId);
            if (currentUser == null || !await _userManager.CheckPasswordAsync(currentUser, currentPassword))
            {
                throw new InvalidOperationException("Invalid a urrent password.");
            }

            if (newPassword != confirmPassword)
            {
                throw new InvalidOperationException("New password and confirmation do not match.");
            }

            var result = await _userManager.ChangePasswordAsync(currentUser, currentPassword, newPassword);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to change password: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            await _userManager.UpdateSecurityStampAsync(currentUser);

            return true;
        }

        public async Task<string?> GeneratePasswordResetUrlAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                {
                    throw new ArgumentException("Invalid email address.", nameof(email));
                }

                var user = await _userManager.FindByEmailAsync(email);
                if (user == null /*|| !(await _userManager.IsEmailConfirmedAsync(user))*/)
                {
                    return null;
                }

                string token = await _userManager.GeneratePasswordResetTokenAsync(user);
                // Encode token for URL safety
                string encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                string resetUrl = $"{_genralConfig.FrontendUrl}/reset-password?email={WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(email))}&token={encodedToken}";
                return resetUrl;
            }
            catch (ArgumentException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to generate password reset URL.", ex);
            }
        }

        public async Task<bool> ResetPasswordAsync(string email, string password, string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Reset token is required.", nameof(token));
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password is required.", nameof(password));
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required.", nameof(email));

            try
            {
                // Decode email from Base64Url if needed, otherwise use as is
                string decodedEmail;
                try
                {
                    var emailDecodedBytes = WebEncoders.Base64UrlDecode(email);
                    decodedEmail = Encoding.UTF8.GetString(emailDecodedBytes);
                }
                catch (FormatException)
                {
                    // If decoding fails, assume email is plain
                    decodedEmail = email;
                }

                var user = await _userManager.FindByEmailAsync(decodedEmail);
                if (user == null)
                {
                    // Do not reveal user existence
                    return false;
                }

                // Decode the token from Base64Url format
                string decodedToken;
                try
                {
                    var decodedBytes = WebEncoders.Base64UrlDecode(token);
                    decodedToken = Encoding.UTF8.GetString(decodedBytes);
                }
                catch (FormatException ex)
                {
                    throw new ArgumentException("Invalid or malformed reset token.", nameof(token), ex);
                }

                var result = await _userManager.ResetPasswordAsync(user, decodedToken, password);
                if (!result.Succeeded)
                    return false;

                await _userManager.UpdateSecurityStampAsync(user);
                return true;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to reset password.", ex);
            }
        }
    }
}