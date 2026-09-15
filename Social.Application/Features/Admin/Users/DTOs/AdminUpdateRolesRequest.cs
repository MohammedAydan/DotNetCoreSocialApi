using System.Collections.Generic;

namespace Social.Application.Features.Admin.Users.DTOs
{
    public class AdminUpdateRolesRequest
    {
        public List<string> Roles { get; set; } = new();
        public string Reason { get; set; } = string.Empty;
    }
}
