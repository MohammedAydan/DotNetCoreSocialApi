using System;

namespace Social.Core.Entities
{
    public class AuditLog
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string AdminId { get; set; } = string.Empty;
        public string? AdminEmail { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string TargetEntity { get; set; } = string.Empty;
        public string TargetId { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    }
}
