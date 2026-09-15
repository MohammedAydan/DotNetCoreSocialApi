namespace Social.Application.Features.Notifications.DTOs
{
    public class NotificationPreferenceDto
    {
        public string UserId { get; set; } = string.Empty;
        public bool LikeEnabled { get; set; } = true;
        public bool CommentEnabled { get; set; } = true;
        public bool ReplyEnabled { get; set; } = true;
        public bool FollowEnabled { get; set; } = true;
        public bool FollowRequestEnabled { get; set; } = true;
        public bool ShareEnabled { get; set; } = true;
        public bool MentionEnabled { get; set; } = true;
        public int? QuietStartHourUtc { get; set; }
        public int? QuietEndHourUtc { get; set; }
        public bool DigestEnabled { get; set; }
    }
}
