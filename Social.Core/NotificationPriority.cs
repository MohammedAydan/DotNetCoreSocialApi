namespace Social.Core
{
    // Write-time priority for smart-inbox ordering (IsRead ASC, Priority DESC, CreatedAt DESC).
    public static class NotificationPriority
    {
        public const int Moderation = 100;
        public const int Mention = 90;
        public const int CommentReply = 70;
        public const int Comment = 60;
        public const int FollowRequest = 55;
        public const int Share = 45;
        public const int Follow = 40;
        public const int AcceptFollow = 40;
        public const int Like = 20;
        public const int Default = 10;

        public static int For(string? type) => type switch
        {
            "ModerationNotice" => Moderation,
            "mention" => Mention,
            NotificationActionTypes.CommentReply => CommentReply,
            NotificationActionTypes.Comment => Comment,
            NotificationActionTypes.FollowRequest => FollowRequest,
            NotificationActionTypes.Share => Share,
            NotificationActionTypes.Follow => Follow,
            NotificationActionTypes.AcceptFollowRequest => AcceptFollow,
            NotificationActionTypes.Unfollow => AcceptFollow,
            NotificationActionTypes.Like => Like,
            _ => Default
        };
    }
}
