using AutoMapper;
using FluentAssertions;
using Social.Application.Features.Users;
using Social.Application.Features.Users.DTOs;
using Social.Core.Entities;
using Xunit;

namespace Social.Tests.Unit.Features.Users
{
    public class UpdateUserVerificationTests
    {
        private readonly IMapper _mapper;

        public UpdateUserVerificationTests()
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile<UserProfile>());
            _mapper = config.CreateMapper();
        }

        [Fact]
        public void Map_OntoVerifiedUser_ShouldPreserveIsVerified()
        {
            // Arrange: verified account in DB
            var existing = new User
            {
                Id = "u1",
                FirstName = "Old",
                LastName = "Name",
                UserName = "user1",
                IsVerified = true,
                IsPrivate = false
            };
            var dto = new UpdateUserDto
            {
                Id = "u1",
                FirstName = "New",
                LastName = "Name",
                UserName = "user1",
                Bio = "hello",
                ProfileImageUrl = "",
                IsPrivate = false
            };

            // Act: overlay DTO onto tracked entity (same pattern a safe handler would use)
            _mapper.Map(dto, existing);

            // Assert: verification survives a profile edit
            existing.IsVerified.Should().BeTrue();
            existing.FirstName.Should().Be("New");
        }

        [Fact]
        public void Map_NewUserInstance_ShouldNotCarryVerifiedTrue()
        {
            // Arrange
            var dto = new UpdateUserDto
            {
                Id = "u1",
                FirstName = "New",
                LastName = "Name",
                UserName = "user1",
                IsPrivate = false
            };

            // Act
            var mapped = _mapper.Map<User>(dto);

            // Assert: fresh mapped instance defaults to unverified (repo must not copy this onto existing)
            mapped.IsVerified.Should().BeFalse();
        }
    }
}
