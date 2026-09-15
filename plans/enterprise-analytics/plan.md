# Enterprise Analytics & Admin Dashboard Overhaul — Plan

## Goal
Ship a backend-only telemetry + daily-aggregation analytics pipeline (MySQL/EF Core) with 5 structured admin REST endpoints, and overhaul the embedded admin console (`/admin`) into an enterprise-grade Executive/System/Users experience — without breaking the 214-test suite or the existing admin-audit trail.

## Acceptance criteria
- `dotnet build Social.sln -c Release` → 0 errors.
- `dotnet test Social.sln -c Release` → 0 failures (existing + new analytics tests).
- New EF migration generates cleanly (MySQL); entities use indexed range scans + `AsNoTracking()`.
- `GET /api/admin/analytics/kpi-summary|user-growth|content-velocity|api-health|safety-metrics` return shaped JSON for Admin/Moderator roles.
- Telemetry middleware captures API traffic via non-blocking Channel + 5s batch flush; excludes health/static/swagger/admin-login GET.
- Daily 00:05 UTC worker aggregates previous-day snapshot (idempotent upsert by unique Date).
- `/admin`, `/admin/dashboard`, `/admin/system`, `/admin/users` render new AdminShell (topbar/sidebar, KPI ribbon with sparklines, Chart.js trends, audit/anomaly tables); old `/admin/moderation|audit-logs|diagnostics` routes keep working.

## Approach
1. **Preserve, don't rewrite**: existing `AuditLog` (admin-action trail, string GUID PK) stays untouched. New `RequestLog` entity carries the mission's telemetry fields (long PK, Endpoint/Method/Status/DurationMs/Ip/UserAgent/UserId/CreatedAt, indexed). New `DailyMetricSnapshot` per spec with unique `Date`.
2. **Hot path**: `RequestTelemetryMiddleware` (Stopwatch.GetTimestamp, claims UserId, path exclusions) → `Channel<RequestLog>` (bounded, drop-on-full) → `RequestLogFlushWorker` batch-inserts every 5s via scoped DbContext.
3. **Cold path**: `MetricsAggregationWorker` (daily 00:05 UTC, hourly backfill of missing days) aggregates Users/Posts/Likes/Comments/RequestLogs into snapshots. `IAnalyticsService` serves all 5 endpoints from snapshots + indexed live fallbacks; MediatR queries stay thin; controllers stay thin.
4. **Frontend**: single-shell upgrade inside live `AdminDashboardController` (the Blazor tree is dead code — not wired). New CSS design tokens (indigo `#4F46E5`, teal `#0D9488`, crimson `#E11D48`, Inter + tabular-nums, `border-border/60`, `p-5/6` cards) + AdminShell layout + 3 redesigned tabs fed by new endpoints via Chart.js CDN (with SVG-sparkline/no-JS fallback).
5. **Verify**: build, full tests, migration SQL spot-check (never `database update` prod without human approval).

## Scope IN
- `RequestLog`, `DailyMetricSnapshot` entities + EF config + migration.
- `RequestTelemetryMiddleware`, `RequestLogChannel`, `RequestLogFlushWorker`, `MetricsAggregationWorker`, `IAnalyticsService` + 5 queries + controller routes.
- AdminShell redesign (topbar/sidebar/KPI ribbon/charts/tables) + CSS tokens.
- Unit tests (service math, range parsing, middleware exclusions) + SQLite repo/worker tests.

## Scope OUT
- Email/push/SignalR delivery, retention cleanup job, admin broadcast, Blazor rewire, prod `database update`, real WebSocket live-traffic (polling only).
- No changes to `AuditLog` schema, ban/block logic, notification intel, or auth flows.

## Dependencies
- .NET 9, Pomelo MySQL 8, MediatR 12.5, existing `ApplicationDbContext` UTC/precision conventions, `CustomWebApplicationFactory` test doubles.
- Chart.js via CDN at runtime (graceful degrade if offline).

## Complexity: XL (backend pipeline M + dashboard overhaul M, one session, staged commits)
