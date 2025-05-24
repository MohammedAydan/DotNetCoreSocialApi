using Social.Application.Features.Like.DTOs;
using Social.Application.Features.Users.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Like.DTOs
{
    public class LikeDto
    {
        public string Id { get; set; }
        public string PostId { get; set; }
        public string UserId { get; set; }
        public LikeUserDto User { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
