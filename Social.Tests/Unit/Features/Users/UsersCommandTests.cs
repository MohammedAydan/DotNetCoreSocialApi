using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Social.Application.Features.Users.Commands;
using Social.Application.Features.Users.DTOs;
using Social.Core.Entities;
using Social.Core.Interfaces;
using Xunit;

namespace Social.Tests.Unit.Features.Users
{
    public class UsersCommandTests
    {
        private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
        private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
        private readonly IMapper _mapper = Substitute.For<IMapper>();
        private readonly IEmailRepository _emailRepository = Substitute.For<IEmailRepository>();
        private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();

        [Fact]
        public async Task CreateUser_WithValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var request = new CreateUserRequest
            {
                Email = "test@example.com",
                UserName = "testuser",
                Password = "Password123!",
                FirstName = "Test",
                LastName = "User",
                UserGender = "male"
            };
            var user = new User
            {
                Id = "user-123",
                Email = request.Email,
                UserName = request.UserName,
                FirstName = request.FirstName,
                LastName = request.LastName,
                UserGender = "male"
            };
            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName
            };

            _mapper.Map<User>(request).Returns(user);
            _mapper.Map<UserDto>(user).Returns(userDto);
            _userRepository.CreateAsync(user, request.Password).Returns(user);
            _userRepository.GetUserRolesAsync(user).Returns(new List<string> { "User" });
            _tokenService.GenerateToken(user, Arg.Any<IList<string>>()).Returns("jwt-access-token");
            _userRepository.CreateRefreshTokenAsync(user.Id).Returns("refresh-token-123");

            var handler = new CreateUserCommandHandler(_userRepository, _mapper, _tokenService);

            // Act
            var result = await handler.Handle(new CreateUserCommand(request), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.AccessToken.Should().Be("jwt-access-token");
            result.RefreshToken.Should().Be("refresh-token-123");
            result.User.Should().NotBeNull();
            result.User!.Id.Should().Be("user-123");
        }

        [Fact]
        public async Task CreateUser_WithNullRequest_ShouldReturnFailure()
        {
            // Arrange
            var handler = new CreateUserCommandHandler(_userRepository, _mapper, _tokenService);

            // Act
            var result = await handler.Handle(new CreateUserCommand(null!), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain("Invalid request");
        }

        [Fact]
        public async Task SignIn_WithValidCredentials_ShouldReturnAuthResponse()
        {
            // Arrange
            var signIn = new SignIn { Email = "user@test.com", Password = "Password123!" };
            var user = new User { Id = "u1", Email = signIn.Email, UserName = "test" };
            var userDto = new UserDto { Id = "u1", Email = signIn.Email, UserName = "test" };

            _userRepository.SignInAsync(signIn).Returns(user);
            _userRepository.GetUserRolesAsync(user).Returns(new List<string> { "User" });
            _tokenService.GenerateToken(user, Arg.Any<IList<string>>()).Returns("access-token");
            _userRepository.CreateRefreshTokenAsync(user.Id).Returns("ref-token");
            _mapper.Map<UserDto>(user).Returns(userDto);

            var handler = new SignInCommandHandler(_userRepository, _mapper, _tokenService);

            // Act
            var result = await handler.Handle(new SignInCommand(signIn), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.AccessToken.Should().Be("access-token");
            result.RefreshToken.Should().Be("ref-token");
        }

        [Fact]
        public async Task RefreshToken_WithValidToken_ShouldRefreshAndReturnNewTokens()
        {
            // Arrange
            var tokenStr = "valid-token";
            var tokenEntity = new RefreshToken
            {
                Token = tokenStr,
                UserId = "u1",
                Expires = DateTime.UtcNow.AddDays(1)
            };
            var user = new User { Id = "u1", Email = "u1@test.com" };
            var userDto = new UserDto { Id = "u1", Email = user.Email };

            _userRepository.ValidateRefreshTokenAsync(tokenStr).Returns(tokenEntity);
            _userRepository.GetUserByIdAsync("u1").Returns(user);
            _userRepository.GetUserRolesAsync(user).Returns(new List<string> { "User" });
            _tokenService.GenerateToken(user, Arg.Any<IList<string>>()).Returns("new-access-token");
            _userRepository.CreateRefreshTokenAsync("u1").Returns("new-refresh-token");
            _mapper.Map<UserDto>(user).Returns(userDto);

            var handler = new RefreshTokenCommandHandler(_userRepository, _tokenService, _mapper);

            // Act
            var result = await handler.Handle(new RefreshTokenCommand(tokenStr), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.AccessToken.Should().Be("new-access-token");
            result.RefreshToken.Should().Be("new-refresh-token");
        }

        [Fact]
        public async Task RefreshToken_WithInvalidOrExpiredToken_ShouldReturnError()
        {
            // Arrange
            _userRepository.ValidateRefreshTokenAsync(Arg.Any<string>()).Returns((RefreshToken?)null);

            var handler = new RefreshTokenCommandHandler(_userRepository, _tokenService, _mapper);

            // Act
            var result = await handler.Handle(new RefreshTokenCommand("bad-token"), CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain("Invalid or expired refresh token");
        }

        [Fact]
        public async Task Logout_WithValidToken_ShouldBlacklistToken()
        {
            // Arrange
            _configuration["Jwt:ExpireTime"].Returns("60");
            var handler = new LogoutCommandHandler(_tokenService, _configuration);

            // Act
            var result = await handler.Handle(new LogoutCommand("test-jwt"), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            await _tokenService.Received(1).BlacklistTokenAsync("test-jwt", TimeSpan.FromMinutes(60));
        }

        [Fact]
        public async Task ChangePassword_WithValidDetails_ShouldReturnTrue()
        {
            // Arrange
            var request = new ChangePasswordRequest
            {
                CurrentPassword = "OldPassword1!",
                NewPassword = "NewPassword2!",
                ConfirmPassword = "NewPassword2!"
            };
            _userRepository.ChangePassword("user-1", request.CurrentPassword, request.NewPassword, request.ConfirmPassword)
                .Returns(true);

            var handler = new ChangePasswordCommandHandler(_userRepository, _emailRepository);

            // Act
            var result = await handler.Handle(new ChangePasswordCommand("user-1", request), CancellationToken.None);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ForgetPassword_WithRegisteredEmail_ShouldSendEmail()
        {
            // Arrange
            var request = new ForgetPasswordRequest { email = "test@example.com" };
            _userRepository.GeneratePasswordResetUrlAsync("test@example.com").Returns("http://reset-url");

            var handler = new ForgetPasswordCommandHandler(_userRepository, _emailRepository);

            // Act
            var result = await handler.Handle(new ForgetPasswordCommand(request), CancellationToken.None);

            // Assert
            result.Should().Be("http://reset-url");
            await _emailRepository.Received(1).SendPasswordResetEmailAsync("test@example.com", "http://reset-url");
        }
    }
}
