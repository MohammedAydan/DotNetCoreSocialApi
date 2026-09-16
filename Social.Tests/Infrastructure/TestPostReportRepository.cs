using Social.Core.Entities;
using Social.Core.Interfaces;
using Social.Core.Reporting;

namespace Social.Tests.Infrastructure
{
    /// <summary>
    /// In-memory IPostReportRepository double for integration tests.
    /// Mirrors PostReportRepository semantics: 1-based pages throw below 1,
    /// limits clamp to 50, queue/mine order CreatedAt DESC then Id DESC.
    /// Owned by Worker-B (post-reporting T5/T7).
    /// </summary>
    public class TestPostReportRepository : IPostReportRepository
    {
        private const int MaxPageLimit = 50;
        private readonly List<PostReport> _reports = new();
        private readonly object _lock = new();

        public Task<PostReport> AddAsync(PostReport report, CancellationToken ct = default)
        {
            lock (_lock)
            {
                if (string.IsNullOrWhiteSpace(report.Id))
                    report.Id = Guid.NewGuid().ToString();
                _reports.Add(report);
            }
            return Task.FromResult(report);
        }

        public Task<PostReport?> GetByIdAsync(string reportId, CancellationToken ct = default)
        {
            lock (_lock)
            {
                return Task.FromResult(_reports.FirstOrDefault(r => r.Id == reportId));
            }
        }

        public Task<(IReadOnlyList<PostReport> Items, int Total)> GetMyReportsAsync(string reporterId, int page, int limit, CancellationToken ct = default)
        {
            ValidatePage(page);
            limit = NormalizeLimit(limit);
            lock (_lock)
            {
                var query = _reports.Where(r => r.ReporterUserId == reporterId);
                var total = query.Count();
                var items = query
                    .OrderByDescending(r => r.CreatedAt)
                    .ThenByDescending(r => r.Id)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .ToList();
                return Task.FromResult<(IReadOnlyList<PostReport>, int)>((items, total));
            }
        }

        public Task<(IReadOnlyList<PostReport> Items, int Total)> GetQueueAsync(string? status, int page, int pageSize, CancellationToken ct = default)
        {
            ValidatePage(page);
            pageSize = NormalizeLimit(pageSize);
            lock (_lock)
            {
                var query = _reports.AsEnumerable();
                if (!string.IsNullOrWhiteSpace(status))
                    query = query.Where(r => string.Equals(r.Status, status, StringComparison.OrdinalIgnoreCase));
                var total = query.Count();
                var items = query
                    .OrderByDescending(r => r.CreatedAt)
                    .ThenByDescending(r => r.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                return Task.FromResult<(IReadOnlyList<PostReport>, int)>((items, total));
            }
        }

        public Task<bool> ExistsOpenAsync(string postId, string reporterId, CancellationToken ct = default)
        {
            lock (_lock)
            {
                return Task.FromResult(_reports.Any(r =>
                    r.PostId == postId &&
                    r.ReporterUserId == reporterId &&
                    r.Status == ReportStatuses.Pending));
            }
        }

        public Task<PostReport> UpdateStatusAsync(string reportId, string status, string? adminId, string? note, CancellationToken ct = default)
        {
            lock (_lock)
            {
                var report = _reports.FirstOrDefault(r => r.Id == reportId)
                    ?? throw new KeyNotFoundException($"PostReport '{reportId}' not found.");
                report.Status = status;
                report.ReviewedAt = DateTime.UtcNow;
                report.ReviewedByAdminId = adminId;
                report.AdminNote = note;
                return Task.FromResult(report);
            }
        }

        public Task<int> GetOpenCountForPostAsync(string postId, CancellationToken ct = default)
        {
            lock (_lock)
            {
                return Task.FromResult(_reports.Count(r =>
                    r.PostId == postId && r.Status == ReportStatuses.Pending));
            }
        }

        public Task<bool> DeleteAsync(string reportId, CancellationToken ct = default)
        {
            lock (_lock)
            {
                var report = _reports.FirstOrDefault(r => r.Id == reportId);
                if (report == null)
                    return Task.FromResult(false);
                _reports.Remove(report);
                return Task.FromResult(true);
            }
        }

        private static void ValidatePage(int page)
        {
            if (page < 1)
                throw new ArgumentException("Page number must be greater than 0.", nameof(page));
        }

        private static int NormalizeLimit(int limit)
        {
            if (limit < 1)
                throw new ArgumentException("Limit must be greater than 0.", nameof(limit));
            return Math.Min(limit, MaxPageLimit);
        }
    }
}
