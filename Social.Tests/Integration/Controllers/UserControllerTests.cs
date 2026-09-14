using FluentAssertions;
using NSubstitute;
using Social.Application.Features.Users.DTOs;
using Social.Core.Common;
using Social.Core.Entities;
using Social.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Social.Tests.Integration.Controllers
{
    public class UserControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public UserControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Register_WithValidData_ShouldReturnOk()
        {
            // Arrange
            var request = new CreateUserRequest
            {
                UserName = "newuser",
                Email = "newuser@example.com",
                Password = "Password123!",
                UserGender = "Male",
                FirstName = "New",
                LastName = "User",
                Bio = "Hello, I am a test user.",
                BirthDate = new DateTime(1995, 1, 1)
            };

            var user = new User
            {
                Id = "u-new",
                UserName = "newuser",
                Email = "newuser@example.com",
                FirstName = "New",
                LastName = "User",
                UserGender = "Male"
            };

            _factory.MockUserRepository.CreateAsync(Arg.Any<User>(), Arg.Any<string>()).Returns(user);
            _factory.MockUserRepository.GetUserRolesAsync(Arg.Any<User>()).Returns(new List<string> { "User" });
            _factory.MockTokenService.GenerateToken(Arg.Any<User>(), Arg.Any<IList<string>>()).Returns("access-token-123");
            _factory.MockUserRepository.CreateRefreshTokenAsync(Arg.Any<string>()).Returns("refresh-token-123");

            // Act
            var response = await _client.PostAsJsonAsync("/api/User/register", request);
            var raw = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK, because: raw);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
            content.Data!.IsSuccess.Should().BeTrue();
            content.Data.AccessToken.Should().Be("access-token-123");
        }

        [Fact]
        public async Task Register_WithEmptyRequiredFields_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new CreateUserRequest
            {
                UserName = "",
                Email = "",
                Password = "",
                UserGender = ""
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/User/register", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task SignIn_WithValidCredentials_ShouldReturnOk()
        {
            // Arrange
            var request = new SignIn
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            var user = new User
            {
                Id = "test-user-id",
                UserName = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };

            _factory.MockUserRepository.SignInAsync(Arg.Any<SignIn>()).Returns(user);
            _factory.MockUserRepository.GetUserRolesAsync(user).Returns(new List<string> { "User" });
            _factory.MockTokenService.GenerateToken(user, Arg.Any<IList<string>>()).Returns("access-token-abc");
            _factory.MockUserRepository.CreateRefreshTokenAsync("test-user-id").Returns("refresh-token-abc");

            // Act
            var response = await _client.PostAsJsonAsync("/api/User/sign-in", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
            content.Data!.AccessToken.Should().Be("access-token-abc");
        }

        [Fact]
        public async Task RefreshToken_WithValidToken_ShouldReturnOk()
        {
            // Arrange
            var request = new RefreshTokenRequest { RefreshToken = "valid-refresh-token" };
            var refreshTokenEntity = new RefreshToken
            {
                Token = "valid-refresh-token",
                UserId = "test-user-id",
                Expires = DateTime.UtcNow.AddDays(7)
            };
            var user = new User
            {
                Id = "test-user-id",
                UserName = "testuser",
                Email = "test@example.com"
            };

            _factory.MockUserRepository.ValidateRefreshTokenAsync("valid-refresh-token").Returns(refreshTokenEntity);
            _factory.MockUserRepository.GetUserByIdAsync("test-user-id").Returns(user);
            _factory.MockUserRepository.GetUserRolesAsync(user).Returns(new List<string> { "User" });
            _factory.MockTokenService.GenerateToken(user, Arg.Any<IList<string>>()).Returns("new-access-token");
            _factory.MockUserRepository.CreateRefreshTokenAsync("test-user-id").Returns("new-refresh-token");

            // Act
            var response = await _client.PostAsJsonAsync("/api/User/refresh-token", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
            content.Data!.AccessToken.Should().Be("new-access-token");
        }

        [Fact]
        public async Task GetUser_WhenAuthenticated_ShouldReturnCurrentUser()
        {
            // Arrange
            var user = new User
            {
                Id = TestAuthHandler.DefaultUserId,
                UserName = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };

            _factory.MockUserRepository.GetUserByIdAsync(TestAuthHandler.DefaultUserId).Returns(user);

            // Act
            var response = await _client.GetAsync("/api/User/get-user");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
            content.Data!.Id.Should().Be(TestAuthHandler.DefaultUserId);
        }

        [Fact]
        public async Task SearchUsers_WithQuery_ShouldReturnMatchingUsers()
        {
            // Arrange
            var users = new List<User>
            {
                new() { Id = "u1", UserName = "search1", Email = "s1@example.com", FirstName = "Search", LastName = "One" },
                new() { Id = "u2", UserName = "search2", Email = "s2@example.com", FirstName = "Search", LastName = "Two" }
            };

            _factory.MockUserRepository.SearchUsers("search", Arg.Any<string?>(), 1, 20)
                .Returns((users, 2));

            // Act
            var response = await _client.GetAsync("/api/User/search?q=search");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<UserDto>>>();
            content.Should().NotBeNull();
            content!.Success.Should().BeTrue();
            content.Data.Should().HaveCount(2);
        }
    }
}
