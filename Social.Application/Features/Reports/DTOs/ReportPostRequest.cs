namespace Social.Application.Features.Reports.DTOs
{
    public class ReportPostRequest
    {
        public string Reason { get; set; } = string.Empty;
        public string? Details { get; set; }
    }
}
