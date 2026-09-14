namespace Social.Core
{
    public static class MediaTypes
    {
        public const string Image = "image";
        public const string Video = "video";
        public const string Audio = "audio";
        public const string Document = "document";
        public const string Text = "text";
        public const string Application = "application";

        [Obsolete("Use NotificationActionTypes instead.")]
        public static class Actions
        {
            public const string Create = NotificationActionTypes.Create;
            public const string Read = NotificationActionTypes.Read;
            public const string Update = NotificationActionTypes.Update;
            public const string Delete = NotificationActionTypes.Delete;
            public const string Like = NotificationActionTypes.Like;
            public const string Comment = NotificationActionTypes.Comment;
            public const string CommentReply = NotificationActionTypes.CommentReply;
            public const string Share = NotificationActionTypes.Share;
            public const string Follow = NotificationActionTypes.Follow; 
            public const string FollowRequest = NotificationActionTypes.FollowRequest;
            public const string AcceptFollowRequest = NotificationActionTypes.AcceptFollowRequest;
            public const string RejectFollowRequest = NotificationActionTypes.RejectFollowRequest;
            public const string CancelFollowRequest = NotificationActionTypes.CancelFollowRequest;
            public const string Accept = NotificationActionTypes.Accept;
            public const string Unfollow = NotificationActionTypes.Unfollow;
            public const string Block = NotificationActionTypes.Block;
            public const string Unblock = NotificationActionTypes.Unblock;
            public const string Report = NotificationActionTypes.Report;
            public const string Mute = NotificationActionTypes.Mute;
            public const string Unmute = NotificationActionTypes.Unmute;
        }
    }
}
