using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Social.Core.Entities
{
    // Per-user notification management settings. One row per user (UserId PK).
    public class NotificationPreference
    {
        [Key]
        [ForeignKey("User")]
        public string UserId { get; set; } = string.Empty;
        public User? User { get; set; }

        // Per-type delivery toggles. ModerationNotice bypasses toggles (always delivered).
        public bool LikeEnabled { get; set; } = true;
        public bool CommentEnabled { get; set; } = true;
        public bool ReplyEnabled { get; set; } = true;
        public bool FollowEnabled { get; set; } = true;
        public bool FollowRequestEnabled { get; set; } = true;
        public bool ShareEnabled { get; set; } = true;
        public bool MentionEnabled { get; set; } = true;

        // Quiet hours window in UTC hours (0-23). Null = disabled.
        // Supports wrap-around (e.g. Start=22, End=7).
        public int? QuietStartHourUtc { get; set; }
        public int? QuietEndHourUtc { get; set; }

        // Digest mode: low-priority events (like/follow/share) collapse into one daily row.
        public bool DigestEnabled { get; set; } = false;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsTypeEnabled(string type) => type switch
        {
            NotificationActionTypes.Like => LikeEnabled,
            NotificationActionTypes.Comment => CommentEnabled,
            NotificationActionTypes.CommentReply => ReplyEnabled,
            NotificationActionTypes.Follow => FollowEnabled,
            NotificationActionTypes.AcceptFollowRequest => FollowEnabled,
            NotificationActionTypes.Unfollow => FollowEnabled,
            NotificationActionTypes.FollowRequest => FollowRequestEnabled,
            NotificationActionTypes.Share => ShareEnabled,
            "mention" => MentionEnabled,
            // Safety-critical: moderation and security notices always delivered.
            "ModerationNotice" => true,
            _ => true
        };

        public bool IsQuietNow(DateTime utcNow)
        {
            if (!QuietStartHourUtc.HasValue || !QuietEndHourUtc.HasValue)
                return false;
            var start = QuietStartHourUtc.Value;
            var end = QuietEndHourUtc.Value;
            if (start < 0 || start > 23 || end < 0 || end > 23 || start == end)
                return false;
            var hour = utcNow.Hour;
            return start < end
                ? hour >= start && hour < end
                : hour >= start || hour < end;
        }
    }
}
