# Context — enterprise-analytics

## Files to create
- `Social.Core/Entities/RequestLog.cs`, `Social.Core/Entities/DailyMetricSnapshot.cs`
- `Social.Core/Interfaces/IAnalyticsService.cs` (analytics contract), `Social.Core/Interfaces/IRequestLogSink.cs` (channel sink)
- `Social.Infrastructure/Telemetry/RequestLogChannel.cs`, `Social.Infrastructure/Telemetry/RequestLogFlushWorker.cs`, `Social.Infrastructure/Telemetry/MetricsAggregationWorker.cs`
- `Social.Infrastructure/Services/AnalyticsService.cs`
- `Social/Middlewares/RequestTelemetryMiddleware.cs`
- `Social.Application/Features/Admin/Analytics/Queries/GetKpiSummaryQuery.cs`, `GetUserGrowthQuery.cs`, `GetContentVelocityQuery.cs`, `GetApiHealthQuery.cs`, `GetSafetyMetricsQuery.cs` (+ DTOs in `DTOs/EnterpriseAnalyticsDtos.cs`)
- `Social.Infrastructure/Migrations/*_AddTelemetryAndDailyMetrics.cs`
- `Social.Tests/Unit/Analytics/EnterpriseAnalyticsTests.cs`

## Files to modify
- `Social.Infrastructure/Data/ApplicationDbContext.cs` (DbSets + index config; AuditLog untouched)
- `Social.Infrastructure/DependencyInjection.cs` (channel singleton, workers, analytics service)
- `Social/Program.cs` (middleware order: after TokenBlacklist, before MapControllers)
- `Social/Controllers/Admin/AdminAnalyticsController.cs` (5 new GET routes; keep overview/diagnostics)
- `Social/Controllers/Admin/AdminDashboardController.cs` (AdminShell + 3 pages; keep legacy tabs/routes)
- `Social.Admin.Web/wwwroot/css/admin-dashboard.css` (design tokens)
- `Social.Tests/Infrastructure/CustomWebApplicationFactory.cs` (only if new services break DI — prefer no change; workers must no-op in Testing env)
- `plans/DECISIONS.md` (ADR-011), `plans/TECH_STACK.md`, `plans/ARCH.md`, `plans/context.md`, `plans/SESSION_LOG.md`

## Key design constraints (discovered)
- `AuditLog` is admin-action trail (string GUID PK, no telemetry fields) with live tests/doubles — MUST NOT change its PK/shape. Mission's telemetry table lands as new `RequestLog`.
- Live dashboard path is `AdminDashboardController` string-HTML (~2000 lines); Blazor tree under `Social.Admin.Web/Components` is dead (no `@page`/router wiring) — overhaul targets the controller shell + shared CSS.
- `CustomWebApplicationFactory` replaces repos with NSubstitute doubles and runs env=Testing; background workers must skip DB work when env is Testing or connection unavailable (catch-and-log).
- MySQL utf8mb4: string FK/indexed cols need `HasMaxLength(255)`; Endpoint 255, UserId 255, Ip 45, UserAgent 512, Method 10.
- Global UTC ValueConverter + precision(6) auto-applies to new DateTime props — no manual config needed.
- Telemetry exclusions: `/admin/login` GET, `/openapi`, `/swagger`, health (`/health`, `/healthz`), `/_content`, static file hits (dot in last segment), OPTIONS preflight.
- Analytics queries: indexed range scans (`CreatedAt >= start && < end`), `AsNoTracking()`, snapshot-first with live fallback.
- Never run `dotnet ef database update` against prod without explicit human approval.

## Open questions
- None blocking. Daily worker time (00:05 UTC) implemented via compute-delay loop; hourly mode not needed beyond missing-day backfill.
- Live-traffic page: polling (5s) rather than WebSocket — matches "or polling" allowance and avoids new infra.
