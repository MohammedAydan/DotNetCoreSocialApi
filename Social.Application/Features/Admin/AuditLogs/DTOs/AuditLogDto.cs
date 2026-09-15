using System;

namespace Social.Application.Features.Admin.AuditLogs.DTOs
{
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
}
