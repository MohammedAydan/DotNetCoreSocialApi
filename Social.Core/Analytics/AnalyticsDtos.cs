namespace Social.Core.Analytics
{
    public sealed record KpiSummaryDto(
        int DauToday,
        double DauDeltaPct,
        int NewUsersToday,
        double NewUsersDeltaPct,
        int TotalUsers,
        int TotalPosts,
        int Interactions24h,
        double EngagementRatePct,
        double P95LatencyMs,
        double ErrorRate24hPct,
        double ErrorRateDeltaPct);

    public sealed record UserGrowthPoint(DateTime Date, int NewUsers, int Dau);
    public sealed record UserGrowthDto(
        string Range,
        int Days,
        List<UserGrowthPoint> Points,
        int Dau,
        int Wau,
        int Mau);

    public sealed record ContentVelocityPoint(
        DateTime Date, int Posts, int Shares, int Deleted);
    public sealed record ContentVelocityDto(
        int Days, List<ContentVelocityPoint> Points, int MediaAttachments);

    public sealed record SlowEndpointDto(
        string Method, string Endpoint, double AvgMs, int Requests, double ErrorPct);
    public sealed record ApiHealthDto(
        double P50Ms, double P90Ms, double P95Ms, double P99Ms,
        int TotalRequests24h,
        int Count2xx, int Count4xx, int Count5xx,
        List<SlowEndpointDto> SlowestEndpoints);

    public sealed record BlockTrendPoint(DateTime Date, int Blocks);
    public sealed record TopBlockedDto(
        string UserId, string UserName, int BlockCount);
    public sealed record SafetyMetricsDto(
        int Days,
        List<BlockTrendPoint> BlocksOverTime,
        int PrivateAccounts,
        int PublicAccounts,
        double PrivateRatioPct,
        int ModerationActions7d,
        double ModerationRatePer1k,
        List<TopBlockedDto> TopBlocked);
    public sealed record RequestStreamItem(
        DateTime At, string Method, string Endpoint, int Status,
        double LatencyMs, string? UserId, string? Ip);
}
