using Social.Application.Features.Reports.DTOs;
using Social.Core.Entities;

namespace Social.Application.Features.Reports
{
    internal static class ReportMapping
    {
        internal const int ExcerptLength = 140;

        internal static readonly string[] ValidReasons =
        [
            Core.Reporting.ReportReasons.Spam,
            Core.Reporting.ReportReasons.Harassment,
            Core.Reporting.ReportReasons.HateSpeech,
            Core.Reporting.ReportReasons.Nudity,
            Core.Reporting.ReportReasons.Violence,
            Core.Reporting.ReportReasons.Misinformation,
            Core.Reporting.ReportReasons.Copyright,
            Core.Reporting.ReportReasons.Other,
        ];

        internal static readonly string[] ValidStatuses =
        [
            Core.Reporting.ReportStatuses.Pending,
            Core.Reporting.ReportStatuses.Dismissed,
            Core.Reporting.ReportStatuses.Actioned,
        ];

        internal static string? ToExcerpt(string? content)
        {
            if (string.IsNullOrEmpty(content))
                return content;
            return content.Length <= ExcerptLength ? content : content.Substring(0, ExcerptLength);
        }

        internal static string? CanonicalReason(string? reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return null;
            return ValidReasons.FirstOrDefault(r =>
                string.Equals(r, reason.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        internal static string? CanonicalStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return null;
            return ValidStatuses.FirstOrDefault(s =>
                string.Equals(s, status.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        internal static PostReportDto ToDto(PostReport report, string? postContent = null, string? postAuthorId = null, int openCount = 0)
        {
            return new PostReportDto
            {
                Id = report.Id,
                PostId = report.PostId,
                PostExcerpt = ToExcerpt(postContent ?? report.Post?.Content),
                PostAuthorId = postAuthorId ?? report.Post?.UserId,
                ReporterUserId = report.ReporterUserId,
                Reason = report.Reason,
                Details = report.Details,
                Status = report.Status,
                CreatedAt = report.CreatedAt,
                ReviewedAt = report.ReviewedAt,
                AdminNote = report.AdminNote,
                OpenCountForPost = openCount
            };
        }
    }
}
