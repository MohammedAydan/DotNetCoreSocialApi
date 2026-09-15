using System;
using System.Collections.Generic;

namespace Social.Admin.Web.Models
{
    public class ModerationItemModel
    {
        public string Id { get; set; } = string.Empty;
        public string ItemType { get; set; } = "Post"; // "Post" or "Comment"
        public string Content { get; set; } = string.Empty;
        public string AuthorId { get; set; } = string.Empty;
        public string AuthorUserName { get; set; } = string.Empty;
        public string? AuthorProfileImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
        public List<ModerationMediaModel> Media { get; set; } = new();
    }

    public class ModerationMediaModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "image";
        public string Url { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
    }

    public class ModerationFilterModel
    {
        public string? ItemType { get; set; } // null = All, "Post", "Comment"
        public bool? IsDeleted { get; set; }
        public bool? HasMedia { get; set; }
        public string? Search { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
    }

    public class ModerationActionModel
    {
        public string ItemId { get; set; } = string.Empty;
        public string ItemType { get; set; } = "Post";
        public bool Hide { get; set; } = true;
        public string Reason { get; set; } = string.Empty;
    }
}
