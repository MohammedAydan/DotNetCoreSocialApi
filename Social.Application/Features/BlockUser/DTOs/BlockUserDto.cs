using Social.Application.Features.Users.DTOs;

namespace Social.Application.Features.BlockUser.DTOs
{
    public class BlockUserDto
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public UserDto BlockedUser { get; set; }
        public DateTime BlockedAt { get; set; }
    }
}