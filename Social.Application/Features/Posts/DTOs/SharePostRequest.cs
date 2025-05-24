using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Posts.DTOs
{
    public class SharePostRequest
    {
        [Required]
        public string ParentPostId { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string Visibility { get; set; } = "Public"; // Default visibility
    }
}
