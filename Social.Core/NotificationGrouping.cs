namespace Social.Core
{
    // Write-time aggregation keys. Repeat events sharing a key collapse into one unread row.
    public static class NotificationGrouping
    {
        public static bool IsAggregatable(string? type) => type is
            NotificationActionTypes.Like or
            NotificationActionTypes.Comment or
            NotificationActionTypes.CommentReply or
            NotificationActionTypes.Follow or
            NotificationActionTypes.FollowRequest or
            NotificationActionTypes.AcceptFollowRequest or
            NotificationActionTypes.Share;

        public static bool IsDigestible(string? type) => type is
            NotificationActionTypes.Like or
            NotificationActionTypes.Follow or
            NotificationActionTypes.Share;

        public static string? BuildGroupKey(string? type, string? postId, string? followerId, string? actorId)
        {
            if (!IsAggregatable(type))
                return null;
            return type switch
            {
                NotificationActionTypes.Like when postId != null => $"like:post:{postId}",
                NotificationActionTypes.Comment when postId != null => $"comment:post:{postId}",
                NotificationActionTypes.CommentReply when postId != null => $"comment:post:{postId}",
                NotificationActionTypes.Share when postId != null => $"share:post:{postId}",
                NotificationActionTypes.Follow when followerId != null => $"follow:{followerId}",
                NotificationActionTypes.FollowRequest when followerId != null => $"followreq:{followerId}",
                NotificationActionTypes.AcceptFollowRequest when actorId != null => $"followaccept:{actorId}",
                _ => null
            };
        }

        public static string BuildDigestKey(string? type, DateTime utcDate) =>
            $"digest:{(type ?? "other").ToLowerInvariant()}:{utcDate:yyyyMMdd}";

        // Human message for an aggregated row. Falls back to the original single-actor text.
        public static string BuildAggregatedMessage(string? type, string actorName, int actorCount, string singleMessage)
        {
            if (actorCount <= 1 || string.IsNullOrWhiteSpace(actorName))
                return singleMessage;
            var others = actorCount - 1;
            var othersText = others == 1 ? "1 other" : $"{others} others";
            return type switch
            {
                NotificationActionTypes.Like => $"{actorName} and {othersText} liked your post.",
                NotificationActionTypes.Comment or NotificationActionTypes.CommentReply
                    => $"{actorName} and {othersText} commented on your post.",
                NotificationActionTypes.Follow => $"{actorName} and {othersText} started following you.",
                NotificationActionTypes.Share => $"{actorName} and {othersText} shared your post.",
                _ => singleMessage
            };
        }
    }
}
