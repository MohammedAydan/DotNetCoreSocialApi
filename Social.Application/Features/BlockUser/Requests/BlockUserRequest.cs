using System.ComponentModel.DataAnnotations;

namespace Social.Application.Features.BlockUser.Requests
{
    public class BlockUserRequest
    {
        [Required]
        public string BlockedUserId { get; set; }
    }
}