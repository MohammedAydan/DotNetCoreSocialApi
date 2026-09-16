using Social.Core.Entities;

namespace Social.Core.Interfaces
{
    public interface IPostReportRepository
    {
        Task<PostReport> AddAsync(PostReport report, CancellationToken ct = default);
        Task<PostReport?> GetByIdAsync(string reportId, CancellationToken ct = default);
        Task<(IReadOnlyList<PostReport> Items, int Total)> GetMyReportsAsync(string reporterId, int page, int limit, CancellationToken ct = default);
        Task<(IReadOnlyList<PostReport> Items, int Total)> GetQueueAsync(string? status, int page, int pageSize, CancellationToken ct = default);
        Task<bool> ExistsOpenAsync(string postId, string reporterId, CancellationToken ct = default);
        // Reports are audit-light rows, not posts: hard delete is allowed (cancel own pending report).
        Task<bool> DeleteAsync(string reportId, CancellationToken ct = default);
        Task<PostReport> UpdateStatusAsync(string reportId, string status, string? adminId, string? note, CancellationToken ct = default);
        Task<int> GetOpenCountForPostAsync(string postId, CancellationToken ct = default);
    }
}
