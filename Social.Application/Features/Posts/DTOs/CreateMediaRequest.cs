using Social.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Posts.DTOs
{
    public class CreateMediaRequest
    {
        public string PostId { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } // image, video, audio, file
        public string Url { get; set; }
        public string ThumbnailUrl { get; set; }
    }
}
