using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Social.Core.Entities;
using Social.Infrastructure.Services;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Social.Tests.Unit.Services
{
    public class DatabaseSeederTests
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseSeeder> _logger;
        private readonly DatabaseSeeder _seeder;

        public DatabaseSeederTests()
        {
            var userStore = Substitute.For<IUserStore<User>>();
            _userManager = Substitute.For<UserManager<User>>(
                userStore, null, null, null, null, null, null, null, null);

            var roleStore = Substitute.For<IRoleStore<IdentityRole>>();
            _roleManager = Substitute.For<RoleManager<IdentityRole>>(
                roleStore, null, null, null, null);

            _configuration = Substitute.For<IConfiguration>();
            _logger = Substitute.For<ILogger<DatabaseSeeder>>();

            _configuration["AdminSeed:Email"].Returns("mohammedaydan12@gmail.com");
            _configuration["AdminSeed:DefaultPassword"].Returns("AdminPassword123!");

            _seeder = new DatabaseSeeder(_userManager, _roleManager, _configuration, _logger);
        }

        [Fact]
        public async Task EnsureAdminUserAsync_WhenUserDoesNotExist_CreatesUserAndAssignsRoles()
        {
            // Arrange
            _roleManager.RoleExistsAsync(Arg.Any<string>()).Returns(true);
            _userManager.FindByEmailAsync("mohammedaydan12@gmail.com").Returns((User?)null);
            _userManager.CreateAsync(Arg.Any<User>(), Arg.Any<string>()).Returns(IdentityResult.Success);
            _userManager.IsInRoleAsync(Arg.Any<User>(), Arg.Any<string>()).Returns(false);
            _userManager.AddToRoleAsync(Arg.Any<User>(), Arg.Any<string>()).Returns(IdentityResult.Success);

            // Act
            var result = await _seeder.EnsureAdminUserAsync("mohammedaydan12@gmail.com", "AdminPassword123!");

            // Assert
            result.Should().BeTrue();
            await _userManager.Received(1).CreateAsync(Arg.Is<User>(u => u.Email == "mohammedaydan12@gmail.com"), "AdminPassword123!");
            await _userManager.Received().AddToRoleAsync(Arg.Any<User>(), "Admin");
            await _userManager.Received().AddToRoleAsync(Arg.Any<User>(), "Moderator");
            await _userManager.Received().AddToRoleAsync(Arg.Any<User>(), "User");
        }

        [Fact]
        public async Task EnsureAdminUserAsync_WhenUserAlreadyExists_EnsuresAdminRolesAndUnlocks()
        {
            // Arrange
            var existingUser = new User
            {
                Id = "existing-user-1",
                Email = "mohammedaydan12@gmail.com",
                UserName = "mohammedaydan12"
            };

            _roleManager.RoleExistsAsync(Arg.Any<string>()).Returns(true);
            _userManager.FindByEmailAsync("mohammedaydan12@gmail.com").Returns(existingUser);
            _userManager.UpdateAsync(existingUser).Returns(IdentityResult.Success);
            _userManager.IsInRoleAsync(existingUser, "Admin").Returns(false);
            _userManager.IsInRoleAsync(existingUser, "Moderator").Returns(false);
            _userManager.IsInRoleAsync(existingUser, "User").Returns(true);
            _userManager.AddToRoleAsync(existingUser, Arg.Any<string>()).Returns(IdentityResult.Success);

            // Act
            var result = await _seeder.EnsureAdminUserAsync("mohammedaydan12@gmail.com", "AdminPassword123!");

            // Assert
            result.Should().BeTrue();
            existingUser.EmailConfirmed.Should().BeTrue();
            existingUser.IsVerified.Should().BeTrue();
            await _userManager.Received(1).AddToRoleAsync(existingUser, "Admin");
            await _userManager.Received(1).AddToRoleAsync(existingUser, "Moderator");
            await _userManager.DidNotReceive().AddToRoleAsync(existingUser, "User");
        }
    }
}
