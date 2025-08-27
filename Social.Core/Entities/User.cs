
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Social.Core.Entities
{
    public class User : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserGender { get; set; } = string.Empty; // Male Or Female
        public DateTime BirthDate { get; set; }
        public string? Bio { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? CoverImageUrl { get; set; }
        public bool IsVerified { get; set; } = false;
        public bool IsPrivate { get; set; } = false;
        public int FollowersCount { get; set; } = 0;
        public int FollowingCount { get; set; } = 0;
        public int PostsCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Post> Posts { get; set; } = new List<Post>();

        [NotMapped]
        public bool IsFollower { get; set; } = false;
        [NotMapped]
        public bool IsFollowerAccepted { get; set; } = false;
        [NotMapped]
        public bool IsFollowing { get; set; } = false;
        [NotMapped]
        public bool IsFollowingAccepted { get; set; } = false;

        // Updated navigation property names for clarity
        public ICollection<Follower> Followers { get; set; } = new List<Follower>(); // People who follow this user
        public ICollection<Follower> Following { get; set; } = new List<Follower>(); // People this user follows

    }
}
