namespace Social.Application.Features.Reports.DTOs
{
    public class ResolveReportRequest
    {
        public string Action { get; set; } = string.Empty;
        public string? Note { get; set; }
    }
}
