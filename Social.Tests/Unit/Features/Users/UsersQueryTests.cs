using AutoMapper;
using FluentAssertions;
using NSubstitute;
using Social.Application.Features.Users.DTOs;
using Social.Application.Features.Users.Queries;
using Social.Core.Entities;
using Social.Core.Interfaces;
using Xunit;

namespace Social.Tests.Unit.Features.Users
{
    public class UsersQueryTests
    {
        private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
        private readonly IMapper _mapper = Substitute.For<IMapper>();

        [Fact]
        public async Task GetUserById_WithValidId_ShouldReturnUserDto()
        {
            // Arrange
            var user = new User { Id = "user-123", UserName = "john" };
            var userDto = new UserDto { Id = "user-123", UserName = "john" };

            _userRepository.GetUserByIdAsync("user-123", "my-user").Returns(user);
            _mapper.Map<UserDto>(user).Returns(userDto);

            var handler = new GetUserByIdQueryHandler(_userRepository, _mapper);

            // Act
            var result = await handler.Handle(new GetUserByIdQuery("user-123", "my-user"), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be("user-123");
            result.UserName.Should().Be("john");
        }

        [Fact]
        public async Task GetUserById_WithNullOrEmptyId_ShouldThrowArgumentNullException()
        {
            // Arrange
            var handler = new GetUserByIdQueryHandler(_userRepository, _mapper);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                handler.Handle(new GetUserByIdQuery("", null), CancellationToken.None));
        }

        [Fact]
        public async Task SearchUsers_WithQueryString_ShouldReturnPaginatedResults()
        {
            // Arrange
            var users = new List<User>
            {
                new() { Id = "u1", UserName = "alex" },
                new() { Id = "u2", UserName = "alexander" }
            };
            var userDtos = new List<UserDto>
            {
                new() { Id = "u1", UserName = "alex" },
                new() { Id = "u2", UserName = "alexander" }
            };

            _userRepository.SearchUsers("alex", "current-user", 1, 10).Returns((users, 2));
            _mapper.Map<IEnumerable<UserDto>>(users).Returns(userDtos);

            var handler = new SearchUsersQueryHandler(_userRepository, _mapper);

            // Act
            var result = await handler.Handle(new SearchUsersQuery("alex", "current-user", 1, 10), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(2);
            result.Results.Should().HaveCount(2);
        }
    }
}
