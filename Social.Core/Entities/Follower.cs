using System;
using System.Text.Json.Serialization;

namespace Social.Core.Entities
{
    public class Follower
    {
        public string Id { get; set; }

        public string FollowerId { get; set; }

        [JsonIgnore]
        public User FollowerUser { get; set; }

        public string FollowingId { get; set; }

        [JsonIgnore]
        public User FollowingUser { get; set; }

        public bool Accepted { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
