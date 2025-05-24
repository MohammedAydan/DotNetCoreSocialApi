using Social.Application.Features.Users.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Comments.DTOs
{
    public class UpdateCommentRequest
    {
        public string Id { get; set; }
        public string Content { get; set; }
    }
}
