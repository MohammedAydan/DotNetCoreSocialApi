using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Social.Core.Entities;
using Social.Core.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Social.Infrastructure.Services
{
    public class DatabaseSeeder : IDatabaseSeeder
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseSeeder> _logger;
        private readonly Data.ApplicationDbContext? _context;

        public DatabaseSeeder(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            ILogger<DatabaseSeeder> logger,
            Data.ApplicationDbContext? context = null)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            if (_context != null && _context.Database.IsRelational())
            {
                try
                {
                    _logger.LogInformation("Applying pending database migrations...");
                    await _context.Database.MigrateAsync(cancellationToken);
                    _logger.LogInformation("Database migrations applied successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while applying database migrations.");
                }
            }

            await EnsureRolesExistAsync();

            var adminEmail = _configuration["AdminSeed:Email"] ?? "mohammedaydan12@gmail.com";
            var defaultPassword = _configuration["AdminSeed:DefaultPassword"] ?? "AdminPassword123!";

            await EnsureAdminUserAsync(adminEmail, defaultPassword, cancellationToken);
        }

        public async Task<bool> EnsureAdminUserAsync(string email, string defaultPassword, CancellationToken cancellationToken = default)
        {
            await EnsureRolesExistAsync();

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                var userName = email.Contains('@') ? email.Split('@')[0] : email;
                user = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = userName,
                    Email = email,
                    FirstName = "Mohammed",
                    LastName = "Aydan",
                    EmailConfirmed = true,
                    IsVerified = true,
                    LockoutEnabled = false
                };

                var createResult = await _userManager.CreateAsync(user, defaultPassword);
                if (!createResult.Succeeded)
                {
                    _logger.LogError("Failed to create admin user {Email}: {Errors}", email, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    return false;
                }

                _logger.LogInformation("Successfully created admin user {Email} with default credentials.", email);
            }
            else
            {
                user.EmailConfirmed = true;
                user.IsVerified = true;
                user.LockoutEnd = null;
                user.AccessFailedCount = 0;
                await _userManager.UpdateAsync(user);
            }

            var roles = new[] { "Admin", "Moderator", "User" };
            foreach (var role in roles)
            {
                if (!await _userManager.IsInRoleAsync(user, role))
                {
                    await _userManager.AddToRoleAsync(user, role);
                    _logger.LogInformation("Assigned role {Role} to user {Email}", role, email);
                }
            }

            return true;
        }

        public async Task<bool> ResetAdminPasswordAsync(string email, string newPassword, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return false;

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);
            if (result.Succeeded)
            {
                await _userManager.UpdateSecurityStampAsync(user);
                _logger.LogInformation("Successfully reset password for admin user {Email}", email);
                return true;
            }

            _logger.LogError("Failed to reset password for {Email}: {Errors}", email, string.Join(", ", result.Errors.Select(e => e.Description)));
            return false;
        }

        private async Task EnsureRolesExistAsync()
        {
            string[] defaultRoles = { "Admin", "Moderator", "User" };
            foreach (var role in defaultRoles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    var result = await _roleManager.CreateAsync(new IdentityRole(role));
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Created system role: {Role}", role);
                    }
                }
            }
        }
    }
}
