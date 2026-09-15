using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Social.Core.Entities
{
    public class BlockUser
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public required string UserId { get; set; }
        
        [Required]
        public required string BlockedUserId { get; set; }
        
        [ForeignKey("UserId")]
        public User? User { get; set; }
        
        [ForeignKey("BlockedUserId")]
        public User? BlockedUser { get; set; }
        
        public DateTime BlockedAt { get; set; } = DateTime.UtcNow;

        public static BlockUser Create(string userId, string blockedUserId)
        {
            return new BlockUser
            {
                UserId = userId,
                BlockedUserId = blockedUserId,
                BlockedAt = DateTime.UtcNow
            };
        }
    }
}