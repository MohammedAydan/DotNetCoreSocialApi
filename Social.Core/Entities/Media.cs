
namespace Social.Core.Entities
{
    public class Media
    {
        public string Id { get; set; }
        public string PostId { get; set; }
        public Post Post { get; set; }

        public string UserId { get; set; }
        public User User { get; set; }

        public string Name { get; set; }
        public string Type { get; set; } // image, video, audio, file
        public string Url { get; set; }
        public string? ThumbnailUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

}
