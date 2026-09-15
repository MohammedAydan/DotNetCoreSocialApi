using System;

namespace Social.Admin.Web.Models
{
    public class SystemDiagnosticsModel
    {
        public bool IsConnected { get; set; }
        public bool UsingMemoryFallback { get; set; }
        public long TrackedIpCount { get; set; }
        public long ThrottledRequestsCount { get; set; }
        public double MemoryWorkingSetMb { get; set; }
        public string Environment { get; set; } = "Production";
        public string FrameworkVersion { get; set; } = ".NET 9.0";
        public TimeSpan SystemUptime { get; set; }
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    }
}
