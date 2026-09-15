using Social.Core.Analytics;

namespace Social.Core.Interfaces
{
    /// <summary>
    /// Read-side analytics over <c>DailyMetricSnapshot</c> rows (snapshot-first)
    /// with indexed live-table fallbacks. All queries use range scans + AsNoTracking.
    /// </summary>
    public interface IAnalyticsService
    {
        Task<KpiSummaryDto> GetKpiSummaryAsync(CancellationToken cancellationToken = default);
        Task<UserGrowthDto> GetUserGrowthAsync(string range = "30d", CancellationToken cancellationToken = default);
        Task<ContentVelocityDto> GetContentVelocityAsync(int days = 30, CancellationToken cancellationToken = default);
        Task<ApiHealthDto> GetApiHealthAsync(CancellationToken cancellationToken = default);
        Task<SafetyMetricsDto> GetSafetyMetricsAsync(int days = 30, CancellationToken cancellationToken = default);
        Task<List<RequestStreamItem>> GetRequestStreamAsync(int take = 50, CancellationToken cancellationToken = default);
    }
}
