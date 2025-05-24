using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Posts.DTOs
{
    public class PaginatedPostsDto
    {
        public IEnumerable<PostDto> Posts { get; set; }
        public int Page { get; set; }
        public int Limit { get; set; }
        public int TotalCount { get; set; }
    }
}
