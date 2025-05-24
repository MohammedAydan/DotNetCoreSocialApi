using Social.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Social.Application.Features.Posts.DTOs
{
    public class CreatePostRequest
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string Visibility { get; set; } = "Public"; // Default visibility

        public ICollection<CreateMediaRequest> Media { get; set; } = [];
    }
}
