namespace Social.Core.Entities
{
    /// <summary>
    /// Daily aggregation snapshot. One row per UTC calendar day (unique <see cref="Date"/>).
    /// Produced by <c>MetricsAggregationWorker</c> (00:05 UTC, idempotent upsert) so
    /// dashboard reads never scan millions of live rows.
    /// </summary>
    public class DailyMetricSnapshot
    {
        public long Id { get; set; }

        /// <summary>UTC midnight of the aggregated day. Unique index.</summary>
        public DateTime Date { get; set; }

        /// <summary>Distinct authenticated UserIds seen in request telemetry that day.</summary>
        public int Dau { get; set; }

        public int NewUsersCount { get; set; }

        public int PostsCreatedCount { get; set; }

        public int SharesCount { get; set; }

        public int LikesCount { get; set; }

        public int CommentsCount { get; set; }

        public double AvgApiLatencyMs { get; set; }

        public int Error5xxCount { get; set; }

        public int Error4xxCount { get; set; }
    }
}
