# Post Reporting — Tasks

- [x] T1 Persistence: `PostReport` entity + `ReportReason`/`ReportStatus` statics + `IPostReportRepository` + DbContext config + additive migration (NOT applied)
- [x] T2 Repository impl: `PostReportRepository` (duplicate-open guard, self-report guard lives in handler, paged mine/queue, status transition, counts)
- [x] T3 Application CQRS: DTOs + `ReportPostCommand`/validator + `GetMyReportsQuery` + `CancelReportCommand` + `GetReportsQueueQuery` + `GetReportByIdQuery` + `ResolveReportCommand`/validator
- [x] T4 User API: `POST /api/posts/{postId}/report`, `GET /api/posts/reports/mine`, `DELETE /api/posts/reports/{reportId}` on `PostsController`
- [x] T5 Admin API: `GET /api/admin/moderation/reports`, `GET .../{reportId}`, `POST .../{reportId}/resolve` on `AdminModerationController` + audit + notifications
- [x] T6 Dashboard: `GET /admin/reports` route + sidebar/nav + reports queue UI (filters, inspect, resolve modals, toasts) + professional polish pass
- [x] T7 Tests: SQLite repo + integration (user + admin + dashboard route); full suite green (242 + new)
- [x] T8 Verify & Close: build 0 errors, tests green, `docs/API_REFERENCE.md` row, `review.md`, SESSION_LOG, living docs (TECH_STACK only if new dep, DECISIONS ADR)
