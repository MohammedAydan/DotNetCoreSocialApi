
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Social.Core.Entities
{
    public class Post
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public User User { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string Visibility { get; set; } = "public"; // Default visibility
        public bool IsDeleted { get; set; } = false;
        public int LikesCount { get; set; } = 0;
        public int ShareingsCount { get; set; } = 0;
        public int CommentsCount { get; set; } = 0;

        public string? ParentPostId { get; set; }
        [ForeignKey("ParentPostId")]
        public Post? ParentPost { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<Media> Media { get; set; } = new List<Media>();
        [JsonIgnore]
        public ICollection<Like> Likes { get; set; } = new List<Like>();
        [JsonIgnore]
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();

        // is liked by the user
        [NotMapped]
        public bool IsLiked { get; set; } = false;
    }
}