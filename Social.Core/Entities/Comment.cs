
using System.ComponentModel.DataAnnotations.Schema;

namespace Social.Core.Entities
{
    public class Comment
    {
        public string Id { get; set; }

        public string PostId { get; set; }
        public Post Post { get; set; }

        public string UserId { get; set; }
        public User User { get; set; }

        public string Content { get; set; }

        public string? ParentId { get; set; }
        public Comment? Parent { get; set; }

        public bool IsDeleted { get; set; } = false;

        public ICollection<Comment> Replies { get; set; }

        public int RepliesCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
