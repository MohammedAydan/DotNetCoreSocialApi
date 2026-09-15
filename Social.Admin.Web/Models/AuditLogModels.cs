using System;

namespace Social.Admin.Web.Models
{
    public class AuditLogItemModel
    {
        public string Id { get; set; } = string.Empty;
        public string AdminId { get; set; } = string.Empty;
        public string? AdminEmail { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string TargetEntity { get; set; } = string.Empty;
        public string TargetId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime TimestampUtc { get; set; }
    }

    public class AuditLogFilterModel
    {
        public string? ActionType { get; set; }
        public string? AdminId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
