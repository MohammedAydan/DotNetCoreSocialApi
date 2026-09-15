using Microsoft.EntityFrameworkCore;
using Social.Core.Analytics;
using Social.Core.Interfaces;
using Social.Infrastructure.Data;

namespace Social.Infrastructure.Services
{
    /// <summary>
    /// Snapshot-first analytics. Daily trend reads prefer <c>DailyMetricSnapshot</c>
    /// rows; today/partial windows use indexed live-table range scans.
    /// Every query is <c>AsNoTracking()</c> with <c>CreatedAt &gt;= start &amp;&amp; &lt; end</c> shapes.
    /// </summary>
    public sealed class AnalyticsService : IAnalyticsService
    {
        private readonly ApplicationDbContext _db;

        public AnalyticsService(ApplicationDbContext db)
        {
            _db = db;
        }

        // ------------------------------------------------ KPI summary ---
        public async Task<KpiSummaryDto> GetKpiSummaryAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            var yesterday = today.AddDays(-1);
            var dayBefore = today.AddDays(-2);

            var dauToday = await DistinctTelemetryUsersAsync(today, now, cancellationToken);
            var dauYesterday = await DistinctTelemetryUsersAsync(yesterday, today, cancellationToken);

            var newToday = await _db.Users.AsNoTracking()
                .Where(u => u.CreatedAt >= today && u.CreatedAt < today.AddDays(1)).CountAsync(cancellationToken);
            var newYesterday = await _db.Users.AsNoTracking()
                .Where(u => u.CreatedAt >= yesterday && u.CreatedAt < today).CountAsync(cancellationToken);

            var totalUsers = await _db.Users.AsNoTracking().CountAsync(cancellationToken);
            var totalPosts = await _db.Posts.AsNoTracking().Where(p => !p.IsDeleted).CountAsync(cancellationToken);

            var windowStart = now.AddHours(-24);
            var likes24 = await _db.Likes.AsNoTracking().Where(l => l.CreatedAt >= windowStart).CountAsync(cancellationToken);
            var comments24 = await _db.Comments.AsNoTracking().Where(c => c.CreatedAt >= windowStart).CountAsync(cancellationToken);
            var shares24 = await _db.Posts.AsNoTracking()
                .Where(p => p.CreatedAt >= windowStart && p.ParentPostId != null).CountAsync(cancellationToken);
            var interactions24 = likes24 + comments24 + shares24;

            var err = await ErrorCountsAsync(windowStart, now, cancellationToken);
            var prev = await ErrorCountsAsync(windowStart.AddHours(-24), windowStart, cancellationToken);
            var errorRate = Pct(err.Bad, err.Total);
            var prevErrorRate = Pct(prev.Bad, prev.Total);

            var p95 = await PercentileAsync(windowStart, now, 0.95, cancellationToken);

            return new KpiSummaryDto(
                DauToday: dauToday,
                DauDeltaPct: DeltaPct(dauToday, dauYesterday),
                NewUsersToday: newToday,
                NewUsersDeltaPct: DeltaPct(newToday, newYesterday),
                TotalUsers: totalUsers,
                TotalPosts: totalPosts,
                Interactions24h: interactions24,
                EngagementRatePct: Math.Round(totalUsers == 0 ? 0 : interactions24 * 100.0 / totalUsers, 2),
                P95LatencyMs: Math.Round(p95, 2),
                ErrorRate24hPct: Math.Round(errorRate, 2),
                ErrorRateDeltaPct: Math.Round(errorRate - prevErrorRate, 2));
        }

        // ------------------------------------------------ user growth ---
        public async Task<UserGrowthDto> GetUserGrowthAsync(string range = "30d", CancellationToken cancellationToken = default)
        {
            var days = ParseRangeDays(range);
            var today = DateTime.UtcNow.Date;
            var start = today.AddDays(-(days - 1));

            var newByDay = await _db.Users.AsNoTracking()
                .Where(u => u.CreatedAt >= start && u.CreatedAt < today.AddDays(1))
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);
            var newMap = newByDay.ToDictionary(x => x.Day, x => x.Count);

            var snaps = await _db.DailyMetricSnapshots.AsNoTracking()
                .Where(s => s.Date >= start && s.Date < today)
                .ToDictionaryAsync(s => s.Date, s => s.Dau, cancellationToken);

            var points = new List<UserGrowthPoint>(days);
            for (var d = start; d <= today; d = d.AddDays(1))
            {
                var isToday = d == today;
                var dau = !isToday && snaps.TryGetValue(d, out var snapDau)
                    ? snapDau
                    : await DistinctTelemetryUsersAsync(d, d.AddDays(1), cancellationToken);
                points.Add(new UserGrowthPoint(d, newMap.TryGetValue(d, out var n) ? n : 0, dau));
            }

            var now = DateTime.UtcNow;
            var wau = await DistinctTelemetryUsersAsync(now.AddDays(-7), now, cancellationToken);
            var mau = await DistinctTelemetryUsersAsync(now.AddDays(-30), now, cancellationToken);

            return new UserGrowthDto(range, days, points, points[^1].Dau, wau, mau);
        }

        // ------------------------------------------- content velocity ---
        public async Task<ContentVelocityDto> GetContentVelocityAsync(int days = 30, CancellationToken cancellationToken = default)
        {
            days = Math.Clamp(days, 1, 365);
            var today = DateTime.UtcNow.Date;
            var start = today.AddDays(-(days - 1));
            var end = today.AddDays(1);

            var posts = await _db.Posts.AsNoTracking()
                .Where(p => p.CreatedAt >= start && p.CreatedAt < end)
                .GroupBy(p => p.CreatedAt.Date)
                .Select(g => new { Day = g.Key, Total = g.Count(), Shares = g.Count(p => p.ParentPostId != null) })
                .ToListAsync(cancellationToken);

            // Soft-deletes are timestamped via UpdatedAt (no DeletedAt column by design).
            var deleted = await _db.Posts.AsNoTracking()
                .Where(p => p.IsDeleted && p.UpdatedAt >= start && p.UpdatedAt < end)
                .GroupBy(p => p.UpdatedAt.Date)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);
            var deletedMap = deleted.ToDictionary(x => x.Day, x => x.Count);
            var postMap = posts.ToDictionary(x => x.Day, x => x);

            var points = new List<ContentVelocityPoint>(days);
            for (var d = start; d <= today; d = d.AddDays(1))
            {
                postMap.TryGetValue(d, out var row);
                points.Add(new ContentVelocityPoint(
                    d, row?.Total ?? 0, row?.Shares ?? 0,
                    deletedMap.TryGetValue(d, out var del) ? del : 0));
            }

            var media = await _db.Media.AsNoTracking()
                .Where(m => m.CreatedAt >= start && m.CreatedAt < end).CountAsync(cancellationToken);

            return new ContentVelocityDto(days, points, media);
        }

        // ------------------------------------------------- api health ---
        public async Task<ApiHealthDto> GetApiHealthAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var start = now.AddHours(-24);

            var base24 = _db.RequestLogs.AsNoTracking()
                .Where(r => r.CreatedAt >= start && r.CreatedAt < now);
            var count2xx = await base24.Where(r => r.StatusCode >= 200 && r.StatusCode <= 299).CountAsync(cancellationToken);
            var count4xx = await base24.Where(r => r.StatusCode >= 400 && r.StatusCode <= 499).CountAsync(cancellationToken);
            var count5xx = await base24.Where(r => r.StatusCode >= 500 && r.StatusCode <= 599).CountAsync(cancellationToken);
            var total = await base24.CountAsync(cancellationToken);

            var slowestRows = await base24
                .GroupBy(r => new { r.HttpMethod, r.Endpoint })
                .Select(g => new
                {
                    g.Key.HttpMethod,
                    g.Key.Endpoint,
                    Avg = g.Average(r => r.DurationMs),
                    Count = g.Count(),
                    Errors = g.Sum(r => r.StatusCode >= 500 ? 1 : 0)
                })
                .OrderByDescending(x => x.Avg)
                .Take(5)
                .ToListAsync(cancellationToken);
            var slowest = slowestRows
                .Select(x => new SlowEndpointDto(
                    x.HttpMethod, x.Endpoint, x.Avg, x.Count,
                    x.Count == 0 ? 0 : x.Errors * 100.0 / x.Count))
                .ToList();

            return new ApiHealthDto(
                P50Ms: Math.Round(await PercentileAsync(start, now, 0.50, cancellationToken), 2),
                P90Ms: Math.Round(await PercentileAsync(start, now, 0.90, cancellationToken), 2),
                P95Ms: Math.Round(await PercentileAsync(start, now, 0.95, cancellationToken), 2),
                P99Ms: Math.Round(await PercentileAsync(start, now, 0.99, cancellationToken), 2),
                TotalRequests24h: total,
                Count2xx: count2xx,
                Count4xx: count4xx,
                Count5xx: count5xx,
                SlowestEndpoints: slowest
                    .Select(s => s with { AvgMs = Math.Round(s.AvgMs, 2), ErrorPct = Math.Round(s.ErrorPct, 2) })
                    .ToList());
        }

        // ---------------------------------------------------- safety ---
        public async Task<SafetyMetricsDto> GetSafetyMetricsAsync(int days = 30, CancellationToken cancellationToken = default)
        {
            days = Math.Clamp(days, 1, 365);
            var now = DateTime.UtcNow;
            var today = now.Date;
            var start = today.AddDays(-(days - 1));

            var blocks = await _db.BlockUsers.AsNoTracking()
                .Where(b => b.BlockedAt >= start && b.BlockedAt < today.AddDays(1))
                .GroupBy(b => b.BlockedAt.Date)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);
            var blockMap = blocks.ToDictionary(x => x.Day, x => x.Count);

            var points = new List<BlockTrendPoint>(days);
            for (var d = start; d <= today; d = d.AddDays(1))
                points.Add(new BlockTrendPoint(d, blockMap.TryGetValue(d, out var c) ? c : 0));

            var privacy = await _db.Users.AsNoTracking()
                .GroupBy(u => u.IsPrivate)
                .Select(g => new { Private = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);
            var privateCount = privacy.Where(x => x.Private).Sum(x => x.Count);
            var publicCount = privacy.Where(x => !x.Private).Sum(x => x.Count);
            var total = privateCount + publicCount;

            var weekAgo = now.AddDays(-7);
            var modActions = await _db.AuditLogs.AsNoTracking()
                .Where(a => a.TimestampUtc >= weekAgo).CountAsync(cancellationToken);

            var topIds = await _db.BlockUsers.AsNoTracking()
                .GroupBy(b => b.BlockedUserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync(cancellationToken);
            var names = await _db.Users.AsNoTracking()
                .Where(u => topIds.Select(t => t.UserId).Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UserName ?? u.Id, cancellationToken);

            return new SafetyMetricsDto(
                Days: days,
                BlocksOverTime: points,
                PrivateAccounts: privateCount,
                PublicAccounts: publicCount,
                PrivateRatioPct: Math.Round(Pct(privateCount, total), 2),
                ModerationActions7d: modActions,
                ModerationRatePer1k: Math.Round(total == 0 ? 0 : modActions * 1000.0 / total, 2),
                TopBlocked: topIds
                    .Select(t => new TopBlockedDto(t.UserId, names.TryGetValue(t.UserId, out var n) ? n : t.UserId, t.Count))
                    .ToList());
        }

        // ---------------------------------------------- request stream ---
        public async Task<List<RequestStreamItem>> GetRequestStreamAsync(int take = 50, CancellationToken cancellationToken = default)
        {
            take = Math.Clamp(take, 1, 200);
            return await _db.RequestLogs.AsNoTracking()
                .OrderByDescending(r => r.CreatedAt)
                .Take(take)
                .Select(r => new RequestStreamItem(
                    r.CreatedAt, r.HttpMethod, r.Endpoint, r.StatusCode,
                    r.DurationMs, r.UserId, r.IpAddress))
                .ToListAsync(cancellationToken);
        }

        // ---------------------------------------------------- helpers ---
        public static int ParseRangeDays(string? range) =>
            range?.ToLowerInvariant() switch
            {
                "7d" => 7,
                "30d" => 30,
                "90d" => 90,
                "1y" => 365,
                _ => 30
            };

        public static double DeltaPct(double current, double previous) =>
            previous == 0 ? (current > 0 ? 100 : 0) : (current - previous) * 100.0 / previous;

        public static double Pct(double part, double total) =>
            total == 0 ? 0 : part * 100.0 / total;

        private async Task<int> DistinctTelemetryUsersAsync(DateTime start, DateTime end, CancellationToken ct)
        {
            var dau = await _db.RequestLogs.AsNoTracking()
                .Where(r => r.CreatedAt >= start && r.CreatedAt < end && r.UserId != null)
                .Select(r => r.UserId!)
                .Distinct()
                .CountAsync(ct);

            if (dau > 0)
                return dau;

            // Fresh-deploy fallback (no telemetry yet): UpdatedAt activity proxy.
            return await _db.Users.AsNoTracking()
                .Where(u => u.UpdatedAt >= start && u.UpdatedAt < end)
                .CountAsync(ct);
        }

        private async Task<(int Total, int Bad)> ErrorCountsAsync(DateTime start, DateTime end, CancellationToken ct)
        {
            var groups = await _db.RequestLogs.AsNoTracking()
                .Where(r => r.CreatedAt >= start && r.CreatedAt < end)
                .GroupBy(r => r.StatusCode >= 400 && r.StatusCode <= 599 ? 1 : 0)
                .Select(g => new { Bad = g.Key, Count = g.Count() })
                .ToListAsync(ct);
            var total = groups.Sum(g => g.Count);
            var bad = groups.Where(g => g.Bad == 1).Sum(g => g.Count);
            return (total, bad);
        }

        private async Task<double> PercentileAsync(DateTime start, DateTime end, double p, CancellationToken ct)
        {
            var n = await _db.RequestLogs.AsNoTracking()
                .Where(r => r.CreatedAt >= start && r.CreatedAt < end).CountAsync(ct);
            if (n == 0)
                return 0;
            var skip = Math.Clamp((int)(n * p), 0, n - 1);
            return await _db.RequestLogs.AsNoTracking()
                .Where(r => r.CreatedAt >= start && r.CreatedAt < end)
                .OrderBy(r => r.DurationMs)
                .Select(r => r.DurationMs)
                .Skip(skip)
                .FirstOrDefaultAsync(ct);
        }
    }
}
