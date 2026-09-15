using System;
using System.Collections.Generic;

namespace Social.Tests.Integration.Admin
{
    public class AdminBanUserRequest
    {
        public string Reason { get; set; } = string.Empty;
        public int? DurationDays { get; set; }
    }

    public class AdminUnbanUserRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class AdminUpdateRolesRequest
    {
        public List<string> Roles { get; set; } = new();
        public string Reason { get; set; } = string.Empty;
    }

    public class AdminToggleVerificationRequest
    {
        public bool IsVerified { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class AdminResetPasswordRequest
    {
        public string? NewPassword { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class AdminModerationActionRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class AdminUpdateVisibilityRequest
    {
        public string Visibility { get; set; } = "public";
        public string Reason { get; set; } = string.Empty;
    }

    public class AdminUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public List<string> Roles { get; set; } = new();
        public bool IsVerified { get; set; }
        public bool IsLockedOut { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public DateTime CreatedAt { get; set; }
        public int PostsCount { get; set; }
        public int FollowersCount { get; set; }
    }

    public class AdminModerationItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "Post" or "Comment"
        public string Content { get; set; } = string.Empty;
        public string AuthorId { get; set; } = string.Empty;
        public string AuthorUserName { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PlatformOverviewMetricsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers24h { get; set; }
        public int TotalPosts { get; set; }
        public int TotalComments { get; set; }
        public int TotalLikes { get; set; }
    }

    public class SystemDiagnosticsDto
    {
        public bool IsConnected { get; set; }
        public bool UsingMemoryFallback { get; set; }
        public long TrackedIpCount { get; set; }
        public long ThrottledRequestsCount { get; set; }
        public double MemoryWorkingSetMb { get; set; }
        public string Environment { get; set; } = string.Empty;
    }

    public class AuditLogDto
    {
        public string Id { get; set; } = string.Empty;
        public string AdminId { get; set; } = string.Empty;
        public string? AdminEmail { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string TargetEntity { get; set; } = string.Empty;
        public string TargetId { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public DateTime TimestampUtc { get; set; }
    }

    public class PaginatedResultDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
