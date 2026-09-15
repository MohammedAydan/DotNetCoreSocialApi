using System;
using System.Collections.Generic;

namespace Social.Application.Features.Admin.Moderation.DTOs
{
    public class AdminMediaDto
    {
        public string Id { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
    }

    public class AdminModerationItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "Post" or "Comment"
        public string? Title { get; set; }
        public string Content { get; set; } = string.Empty;
        public string AuthorId { get; set; } = string.Empty;
        public string AuthorUserName { get; set; } = string.Empty;
        public string? AuthorEmail { get; set; }
        public string? Visibility { get; set; }
        public bool IsDeleted { get; set; }
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
        public int SharesCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<AdminMediaDto> Media { get; set; } = new();
    }
}
