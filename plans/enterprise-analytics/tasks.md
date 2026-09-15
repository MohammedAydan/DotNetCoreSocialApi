# Tasks — enterprise-analytics

## Backend pipeline
- [x] 1. Core entities: `RequestLog`, `DailyMetricSnapshot` (+ `IAnalyticsService`, `IRequestLogSink` contracts)
- [x] 2. DbContext: DbSets + indexes (RequestLog: CreatedAt, UserId, Endpoint, StatusCode; Snapshot: unique Date) + migration
- [x] 3. Telemetry: `RequestTelemetryMiddleware` + `RequestLogChannel` + `RequestLogFlushWorker` (5s batch) + Program wiring
- [x] 4. Aggregation: `MetricsAggregationWorker` (00:05 UTC daily + missing-day backfill, idempotent upsert)
- [x] 5. Service: `AnalyticsService` (kpi/user-growth/content-velocity/api-health/safety) + 5 MediatR queries + controller routes
- [x] 6. Tests: service math/range tests + middleware exclusion tests + SQLite worker/snapshot tests

## Dashboard overhaul
- [x] 7. Design tokens: enterprise CSS (`Social.Admin.Web/wwwroot/css/admin-dashboard.css` + controller inline shell)
- [x] 8. AdminShell: topbar (env badge, health heartbeat poll, range switcher, Cmd+K, profile) + collapsible sidebar (Executive/Audience/Content/Infrastructure)
- [x] 9. Pages: Executive Overview (KPI ribbon+sparklines, DAU vs content chart, donut, anomalies table), System Observability (P50/P90/P95/P99, endpoints matrix, live audit stream), User Intelligence & Safety (growth bars, privacy meter, block density + actions)
- [x] 10. Wire pages to new endpoints with polling + pagination; keep legacy routes working

## Close
- [x] 11. Verify: build 0 errors, full tests pass, migration SQL sane; ADR + living docs + session log
