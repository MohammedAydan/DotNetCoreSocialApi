namespace Social.Core.Entities
{
    /// <summary>
    /// Server-side HTTP telemetry record. Written by <c>RequestTelemetryMiddleware</c>
    /// via a bounded channel and flushed in batches every 5 seconds.
    /// Distinct from <see cref="AuditLog"/> (admin-action trail) — this table is
    /// append-only request telemetry and never carries admin moderation semantics.
    /// </summary>
    public class RequestLog
    {
        public long Id { get; set; }

        /// <summary>Authenticated user id (sub/NameIdentifier claim), null for anonymous.</summary>
        public string? UserId { get; set; }

        /// <summary>Request path without query string, truncated to 255 chars.</summary>
        public string Endpoint { get; set; } = string.Empty;

        public string HttpMethod { get; set; } = string.Empty;

        public int StatusCode { get; set; }

        public double DurationMs { get; set; }

        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
