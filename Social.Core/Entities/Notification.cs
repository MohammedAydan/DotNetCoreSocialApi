using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Social.Core.Entities
{
    public class Notification
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // The user who triggers the notification (e.g., someone who liked or followed)
        [ForeignKey("SenderUser")]
        public string? UserId { get; set; }
        public User? SenderUser { get; set; }

        // The user who receives the notification
        [Required]
        [ForeignKey("RecipientUser")]
        public string RecipientId { get; set; }
        public User RecipientUser { get; set; }

        // Type of notification: Follow, Like, Comment, Mention, etc.
        [Required]
        public string Type { get; set; } = string.Empty;

        // Custom message to show in UI
        public string Message { get; set; } = string.Empty;

        // Optional IDs if the notification relates to specific entities
        public string? PostId { get; set; }
        public string? CommentId { get; set; }
        public string? LikeId { get; set; }
        public string? FollowId { get; set; }
        public string? FollowerId { get; set; }

        // Optional image associated with the notification (e.g., post thumbnail)
        public string? ImageUrl { get; set; }

        // Read status
        public bool IsRead { get; set; } = false;

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Optional: Soft delete flag
        public bool IsDeleted { get; set; } = false;
    }
}
