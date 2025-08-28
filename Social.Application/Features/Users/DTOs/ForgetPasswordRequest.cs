using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Users.DTOs
{
    public class ForgetPasswordRequest
    {
        [Required]
        public string email { get; set; } = null!;
    }
}
