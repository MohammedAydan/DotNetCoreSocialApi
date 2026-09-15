# Review — enterprise-analytics

## What was built
- **Telemetry pipeline**: `RequestLog` + `DailyMetricSnapshot` entities (indexed, additive migration `AddTelemetryAndDailyMetrics` — NOT applied to prod); `RequestTelemetryMiddleware` (`/api`-only, Stopwatch timestamps, never throws) → bounded Channel (10k drop-write) → `RequestLogFlushWorker` (5s batches); `MetricsAggregationWorker` (00:05 UTC, idempotent upsert, 30-day backfill). Both workers no-op in Testing env.
- **Analytics API**: `IAnalyticsService` (`AnalyticsService`, snapshot-first, `AsNoTracking`, range scans) + 6 thin MediatR queries; `GET /api/admin/analytics/kpi-summary|user-growth?range=|content-velocity|api-health|safety-metrics|request-stream` (Admin/Moderator).
- **Dashboard overhaul**: enterprise tokens (indigo/teal/crimson, tabular-nums, light/dark toggle), AdminShell (env badge, 30s heartbeat, 7D/30D switcher, Cmd+K palette, collapsible Executive/Audience/Content/Infrastructure sidebar), Executive (KPI ribbon + SVG sparklines, DAU-vs-content trend, content donut, anomaly table), System (P50–P99, sortable endpoint matrix, 15s live audit stream with filter), Users (growth bars, privacy meter, block density with Inspect/Restrict). New aliases `/admin/dashboard`, `/admin/system`; legacy routes intact.

## Verification
- `dotnet build Social.sln -c Release`: 0 errors (144 pre-existing warnings).
- `dotnet test Social.sln -c Release`: 242/242 passing (214 baseline + 28 new: range/delta math, middleware capture/exclusion incl. live log write, SQLite KPI/growth/health/safety).
- Migration `Up` reviewed: `DailyMetricSnapshots` (unique Date) + `RequestLogs` (6 indexes), `datetime(6)`, utf8mb4 lengths; never `database update` on prod without human approval.
- All 5 admin HTML routes render 200 in integration tests (proves the enlarged shell has no C# interpolation breakage).

## Edge cases handled
- Empty telemetry (fresh deploy): DAU falls back to `UpdatedAt` activity proxy; percentiles return 0; matrices/tables render empty-states, not 500s.
- Channel full → drop (hot path never blocks); middleware exceptions swallowed; flush/aggregate failures logged + retried next tick.
- `statusCode/100` integer-division and predicated-`Count` EF translations avoided (SQLite-verified portable rewrites).
- Chart.js offline → fallback notice; KPIs/tables stay live.

## Known limitations / follow-ups
- Live traffic is polling (heartbeat 30s, audit 15s), not WebSocket.
- No retention cleanup job for `RequestLogs` (table grows ~1 row/request) — needs a follow-up purge/archival worker + index on (CreatedAt) already present.
- No integration test hits the new analytics JSON endpoints (real service needs MySQL; factory uses mock doubles) — covered by SQLite service tests instead.
- `AdminAnalyticsController` previously allowed Moderator; new endpoints inherit that — intended.
- Two legacy test titles updated to the renamed page ("System Observability & API Health").
