using Social.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Posts.DTOs
{
    public class UpdatePostRequest
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string Visibility { get; set; } = "public"; // Default visibility

        public ICollection<MediaDto> Media { get; set; } = [];
    }
}
