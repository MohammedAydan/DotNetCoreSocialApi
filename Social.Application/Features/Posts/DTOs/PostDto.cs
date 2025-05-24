using Social.Application.Features.Users.DTOs;
using Social.Core.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Social.Application.Features.Posts.DTOs
{
    public class PostDto
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public PostUserDto User { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Visibility { get; set; }
        public int LikesCount { get; set; }
        public int ShareingsCount { get; set; }
        public int CommentsCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<MediaDto>? Media { get; set; } = [];
        public bool IsLiked { get; set; } = false;

        public string? ParentPostId { get; set; }
        public PostDto? ParentPost { get; set; }
    }
}
