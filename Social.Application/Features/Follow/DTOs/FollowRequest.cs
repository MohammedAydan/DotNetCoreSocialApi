using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Follow.DTOs
{
    public class FollowRequest
    {
        public string FollowerId { get; set; }
        public string TargetUserId { get; set; }
    }
}
