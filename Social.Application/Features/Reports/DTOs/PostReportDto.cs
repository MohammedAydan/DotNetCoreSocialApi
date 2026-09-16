namespace Social.Application.Features.Reports.DTOs
{
    public class PostReportDto
    {
        public string Id { get; set; } = string.Empty;
        public string PostId { get; set; } = string.Empty;
        public string? PostExcerpt { get; set; }
        public string? PostAuthorId { get; set; }
        public string ReporterUserId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? AdminNote { get; set; }
        public int OpenCountForPost { get; set; }
    }
}
