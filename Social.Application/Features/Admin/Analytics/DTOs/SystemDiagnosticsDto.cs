namespace Social.Application.Features.Admin.Analytics.DTOs
{
    public class SystemDiagnosticsDto
    {
        public bool IsConnected { get; set; }
        public bool UsingMemoryFallback { get; set; }
        public long TrackedIpCount { get; set; }
        public long ThrottledRequestsCount { get; set; }
        public double MemoryWorkingSetMb { get; set; }
        public string Environment { get; set; } = string.Empty;
    }
}
