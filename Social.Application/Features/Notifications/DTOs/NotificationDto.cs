using Social.Application.Features.Users.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Notifications.DTOs
{
    public class NotificationDto
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string? RecipientId { get; set; }
        public UserDto SenderUser { get; set; }
        //public UserDto RecipientUser { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
        public string? PostId { get; set; }
        public string? CommentId { get; set; }
        public string? FollowerId { get; set; }
        public string? LikeId { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsRead { get; set; }
        public bool IsDeferred { get; set; }
        public string? GroupKey { get; set; }
        public int ActorCount { get; set; } = 1;
        public string? LastActorName { get; set; }
        public int Priority { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
