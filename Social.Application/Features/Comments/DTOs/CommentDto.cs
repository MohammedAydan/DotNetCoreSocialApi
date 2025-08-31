using Social.Application.Features.Users.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Comments.DTOs
{
    public class CommentDto
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PostId { get; set; }
        public string UserId { get; set; }
        public CommentUserDto User { get; set; }
        public string Content { get; set; }
        public string ParentId { get; set; }
        public int RepliesCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
