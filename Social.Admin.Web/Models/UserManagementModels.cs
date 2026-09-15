using System;
using System.Collections.Generic;

namespace Social.Admin.Web.Models
{
    public class AdminUserListItemModel
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string? ProfileImageUrl { get; set; }
        public bool IsVerified { get; set; }
        public bool IsLockedOut { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public List<string> Roles { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public class UserFilterModel
    {
        public string? Query { get; set; }
        public string? Role { get; set; }
        public bool? IsLocked { get; set; }
        public bool? IsVerified { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
    }

    public class BanUserModalModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public int DurationDays { get; set; } = 7;
        public bool IsPermanent { get; set; } = false;
    }

    public class UpdateRolesModalModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public bool IsModerator { get; set; }
        public bool IsUser { get; set; } = true;
    }

    public class ResetPasswordModalModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class AdminLoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
