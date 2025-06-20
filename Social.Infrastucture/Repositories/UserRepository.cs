﻿using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Social.Core.Entities;
using Social.Core.Interfaces;
using Social.Infrastucture.Data;
using System;
using System.Security.Authentication;

namespace Social.Infrastucture.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public UserRepository(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _context = context ?? throw new ArgumentNullException(nameof(context));
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
    }
}