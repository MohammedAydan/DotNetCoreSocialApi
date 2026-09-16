using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Social.Core.Entities
{
    public class PostReport
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public required string PostId { get; set; }

        [Required]
        public required string ReporterUserId { get; set; }

        [Required]
        public required string Reason { get; set; }

        public string? Details { get; set; }

        public string Status { get; set; } = Reporting.ReportStatuses.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReviewedAt { get; set; }

        public string? ReviewedByAdminId { get; set; }

        public string? AdminNote { get; set; }

        [ForeignKey("PostId")]
        public Post? Post { get; set; }

        [ForeignKey("ReporterUserId")]
        public User? Reporter { get; set; }
    }
}
