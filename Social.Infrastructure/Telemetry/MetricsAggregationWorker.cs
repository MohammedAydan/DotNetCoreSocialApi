using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Social.Core.Entities;
using Social.Infrastructure.Data;

namespace Social.Infrastructure.Telemetry
{
    /// <summary>
    /// Aggregates the previous UTC day from AuditLogs(RequestLogs)/Posts/Users/
    /// Likes/Comments into <see cref="DailyMetricSnapshot"/> once daily at 00:05 UTC.
    /// Idempotent upsert keyed by the unique <c>Date</c> column; on startup it also
    /// backfills any missing days in the trailing 30-day window. Skips all DB work
    /// in the Testing environment.
    /// </summary>
    public sealed class MetricsAggregationWorker : BackgroundService
    {
        private const int BackfillDays = 30;

        private readonly IServiceScopeFactory _scopes;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<MetricsAggregationWorker> _logger;

        public MetricsAggregationWorker(
            IServiceScopeFactory scopes,
            IHostEnvironment environment,
            ILogger<MetricsAggregationWorker> logger)
        {
            _scopes = scopes;
            _environment = environment;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await BackfillMissingDaysAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Metrics backfill skipped at startup.");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = TimeUntilNextRunUtc(DateTime.UtcNow);
                await Task.Delay(delay, stoppingToken);
                try
                {
                    await AggregateDayAsync(DateTime.UtcNow.Date.AddDays(-1), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Daily metrics aggregation failed.");
                }
            }
        }

        public static TimeSpan TimeUntilNextRunUtc(DateTime nowUtc)
        {
            var next = nowUtc.Date.AddDays(1).AddMinutes(5); // 00:05 UTC
            var delay = next - nowUtc;
            return delay <= TimeSpan.Zero ? TimeSpan.FromMinutes(1) : delay;
        }

        private async Task BackfillMissingDaysAsync(CancellationToken cancellationToken)
        {
            if (_environment.IsEnvironment("Testing"))
                return;

            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var today = DateTime.UtcNow.Date;
            var existing = await db.DailyMetricSnapshots
                .AsNoTracking()
                .Where(s => s.Date >= today.AddDays(-BackfillDays) && s.Date < today)
                .Select(s => s.Date)
                .ToListAsync(cancellationToken);

            for (var d = 1; d <= BackfillDays; d++)
            {
                var day = today.AddDays(-d);
                if (!existing.Contains(day))
                    await AggregateDayAsync(day, cancellationToken);
            }
        }

        internal async Task AggregateDayAsync(DateTime day, CancellationToken cancellationToken)
        {
            if (_environment.IsEnvironment("Testing"))
                return;

            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var start = day.Date;
            var end = start.AddDays(1);

            var dau = await db.RequestLogs
                .AsNoTracking()
                .Where(r => r.CreatedAt >= start && r.CreatedAt < end && r.UserId != null)
                .Select(r => r.UserId!)
                .Distinct()
                .CountAsync(cancellationToken);

            var newUsers = await db.Users
                .AsNoTracking()
                .Where(u => u.CreatedAt >= start && u.CreatedAt < end)
                .CountAsync(cancellationToken);

            var postsCreated = await db.Posts
                .AsNoTracking()
                .Where(p => p.CreatedAt >= start && p.CreatedAt < end)
                .CountAsync(cancellationToken);

            var shares = await db.Posts
                .AsNoTracking()
                .Where(p => p.CreatedAt >= start && p.CreatedAt < end && p.ParentPostId != null)
                .CountAsync(cancellationToken);

            var likes = await db.Likes
                .AsNoTracking()
                .Where(l => l.CreatedAt >= start && l.CreatedAt < end)
                .CountAsync(cancellationToken);

            var comments = await db.Comments
                .AsNoTracking()
                .Where(c => c.CreatedAt >= start && c.CreatedAt < end)
                .CountAsync(cancellationToken);

            var telemetry = await db.RequestLogs
                .AsNoTracking()
                .Where(r => r.CreatedAt >= start && r.CreatedAt < end)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Avg = g.Average(r => r.DurationMs),
                    E5xx = g.Count(r => r.StatusCode >= 500 && r.StatusCode <= 599),
                    E4xx = g.Count(r => r.StatusCode >= 400 && r.StatusCode <= 499)
                })
                .FirstOrDefaultAsync(cancellationToken);

            var existing = await db.DailyMetricSnapshots
                .FirstOrDefaultAsync(s => s.Date == start, cancellationToken);

            if (existing is null)
            {
                db.DailyMetricSnapshots.Add(new DailyMetricSnapshot
                {
                    Date = start,
                    Dau = dau,
                    NewUsersCount = newUsers,
                    PostsCreatedCount = postsCreated,
                    SharesCount = shares,
                    LikesCount = likes,
                    CommentsCount = comments,
                    AvgApiLatencyMs = telemetry?.Avg ?? 0,
                    Error5xxCount = telemetry?.E5xx ?? 0,
                    Error4xxCount = telemetry?.E4xx ?? 0
                });
            }
            else
            {
                existing.Dau = dau;
                existing.NewUsersCount = newUsers;
                existing.PostsCreatedCount = postsCreated;
                existing.SharesCount = shares;
                existing.LikesCount = likes;
                existing.CommentsCount = comments;
                existing.AvgApiLatencyMs = telemetry?.Avg ?? 0;
                existing.Error5xxCount = telemetry?.E5xx ?? 0;
                existing.Error4xxCount = telemetry?.E4xx ?? 0;
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
