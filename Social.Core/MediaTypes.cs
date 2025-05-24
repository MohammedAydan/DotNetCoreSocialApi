using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public class Actions
        {
            public const string Create = "create";
            public const string Read = "read";
            public const string Update = "update";
            public const string Delete = "delete";
            public const string Like = "like";
            public const string Comment = "comment";
            public const string CommentReply = "comment-reply";
            public const string Share = "share";
            public const string Follow = "follow"; 
            public const string FollowRequest = "follow-request";
            public const string AcceptFollowRequest = "accept-follow-request";
            public const string RejectFollowRequest = "reject-follow-request";
            public const string CancelFollowRequest = "cancel-follow-request";
            public const string Accept = "accept";
            public const string Unfollow = "unfollow";
            public const string Block = "block";
            public const string Unblock = "unblock";
            public const string Report = "report";
            public const string Mute = "mute";
            public const string Unmute = "unmute";
        }
    }
}
