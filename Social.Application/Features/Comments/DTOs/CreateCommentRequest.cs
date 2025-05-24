using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Comments.DTOs
{
    public class CreateCommentRequest
    {
        public string PostId { get; set; }
        public string Content { get; set; }
    }
}
