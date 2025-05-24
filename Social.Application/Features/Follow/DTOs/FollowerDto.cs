using Social.Application.Features.Users.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Followers.DTOs
{
    public class FollowerDto
    {
        public string Id { get; set; }

        public string FollowerId { get; set; }

        public string FollowingId { get; set; }

        public bool Accepted { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public UserSummaryDto? Follower { get; set; }

        public UserSummaryDto? Following { get; set; }
    }

    public class UserSummaryDto
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? UserName { get; set; }
        public string? ProfileImageUrl { get; set; }
    }
}
