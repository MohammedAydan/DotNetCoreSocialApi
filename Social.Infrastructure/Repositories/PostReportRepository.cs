using Microsoft.EntityFrameworkCore;
using Social.Core.Entities;
using Social.Core.Interfaces;
using Social.Core.Reporting;
using Social.Infrastructure.Data;

namespace Social.Infrastructure.Repositories
{
    public class PostReportRepository(ApplicationDbContext dbContext) : IPostReportRepository
    {
        private const int MaxPageLimit = 50;

        private readonly ApplicationDbContext _dbContext = dbContext;

        public async Task<PostReport> AddAsync(PostReport report, CancellationToken ct = default)
        {
            await _dbContext.PostReports.AddAsync(report, ct);
            await _dbContext.SaveChangesAsync(ct);
            return report;
        }

        public async Task<PostReport?> GetByIdAsync(string reportId, CancellationToken ct = default)
        {
            return await _dbContext.PostReports
                .Include(r => r.Post)
                .Include(r => r.Reporter)
                .FirstOrDefaultAsync(r => r.Id == reportId, ct);
        }

        public async Task<(IReadOnlyList<PostReport> Items, int Total)> GetMyReportsAsync(string reporterId, int page, int limit, CancellationToken ct = default)
        {
            ValidatePage(page);
            limit = NormalizeLimit(limit);

            var query = _dbContext.PostReports
                .AsNoTracking()
                .Where(r => r.ReporterUserId == reporterId)
                .Include(r => r.Post);

            var total = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .ThenByDescending(r => r.Id)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync(ct);

            return (items, total);
        }

        public async Task<(IReadOnlyList<PostReport> Items, int Total)> GetQueueAsync(string? status, int page, int pageSize, CancellationToken ct = default)
        {
            ValidatePage(page);
            pageSize = NormalizeLimit(pageSize);

            var query = _dbContext.PostReports
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(r => r.Status == status);

            var total = await query.CountAsync(ct);

            var items = await query
                .Include(r => r.Post)
                .Include(r => r.Reporter)
                .OrderByDescending(r => r.CreatedAt)
                .ThenByDescending(r => r.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, total);
        }

        public async Task<bool> ExistsOpenAsync(string postId, string reporterId, CancellationToken ct = default)
        {
            return await _dbContext.PostReports
                .AnyAsync(
                    r => r.PostId == postId
                        && r.ReporterUserId == reporterId
                        && r.Status == ReportStatuses.Pending,
                    ct);
        }

        public async Task<PostReport> UpdateStatusAsync(string reportId, string status, string? adminId, string? note, CancellationToken ct = default)
        {
            if (status != ReportStatuses.Pending
                && status != ReportStatuses.Dismissed
                && status != ReportStatuses.Actioned)
                throw new ArgumentException($"Invalid report status '{status}'.", nameof(status));

            var report = await _dbContext.PostReports
                .FirstOrDefaultAsync(r => r.Id == reportId, ct)
                ?? throw new KeyNotFoundException($"PostReport '{reportId}' not found.");

            report.Status = status;
            report.ReviewedAt = DateTime.UtcNow;
            report.ReviewedByAdminId = adminId;
            report.AdminNote = note;

            await _dbContext.SaveChangesAsync(ct);
            return report;
        }

        public async Task<int> GetOpenCountForPostAsync(string postId, CancellationToken ct = default)
        {
            return await _dbContext.PostReports
                .CountAsync(
                    r => r.PostId == postId
                        && r.Status == ReportStatuses.Pending,
                    ct);
        }

        public async Task<bool> DeleteAsync(string reportId, CancellationToken ct = default)
        {
            var report = await _dbContext.PostReports
                .FirstOrDefaultAsync(r => r.Id == reportId, ct);
            if (report == null)
                return false;

            _dbContext.PostReports.Remove(report);
            await _dbContext.SaveChangesAsync(ct);
            return true;
        }

        /// <summary>
        /// Throws unless <paramref name="page"/> is a 1-based page number.
        /// Pagination inputs throw instead of clamping so client bugs surface
        /// loudly; only the upper <c>limit</c> bound is clamped (see
        /// <see cref="NormalizeLimit"/>). Middleware maps this to 400.
        /// Side effects: none.
        /// </summary>
        private static void ValidatePage(int page)
        {
            if (page < 1)
                throw new ArgumentException("Page number must be greater than 0.", nameof(page));
        }

        /// <summary>
        /// Validates <paramref name="limit"/> and caps it at 50. Throws when less than 1;
        /// values above the cap are clamped (not thrown) to protect the server from
        /// oversized pages. Side effects: none.
        /// </summary>
        private static int NormalizeLimit(int limit)
        {
            if (limit < 1)
                throw new ArgumentException("Limit must be greater than 0.", nameof(limit));
            return Math.Min(limit, MaxPageLimit);
        }
    }
}
