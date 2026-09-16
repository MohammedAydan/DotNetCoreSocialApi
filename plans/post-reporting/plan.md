# Post Reporting + Admin Reports Console — Plan

## Goal
Enable users to report a specific post for policy violations, and give Admin/Moderator a full reports triage workflow (queue, review, resolve/dismiss with audit + notifications) plus a professional `/admin/reports` dashboard page.

## Acceptance Criteria (testable)
1. `POST /api/posts/{postId}/report` with `{reason, details?}` → 200 envelope; duplicate open report by same user+post → 400; self-report own post → 400; missing/deleted post → 404; unauthenticated → 401.
2. `GET /api/posts/reports/mine?page&limit` returns caller's reports ordered `CreatedAt DESC`; `DELETE /api/posts/reports/{reportId}` cancels own pending report.
3. `GET /api/admin/moderation/reports?status?&page&pageSize` (Admin,Moderator) paged queue with post excerpt + reporter + counts; `GET /api/admin/moderation/reports/{reportId}` single; `POST .../resolve` (action: dismiss|hide_post|restore_visibility note) → updates status, writes AuditLog, notifies reporter (+ author on hide); invalid transition → 400; foreign id → 404; non-admin → 401/403.
4. Dashboard `GET /admin/reports` returns 200 HTML for Admin (redirect to login when anonymous); sidebar has Reports entry; queue table with status filter, resolve/dismiss modals, toasts; no console errors.
5. `dotnet build Social.sln -c Release` 0 errors; `dotnet test Social.sln -c Release` 242 baseline + new tests green; new EF migration generated but NOT applied to prod.

## Approach
- New `PostReport` entity in Core (string GUID Id, PostId[255], ReporterUserId[255], Reason[32], Details[1000?], Status[16]=Pending, CreatedAt/ReviewedAt UTC precision-6, ReviewedByAdminId[255?]).
- DbContext config: single-col FK indexes on PostId + ReporterUserId (MySQL 1553), composite `(PostId,ReporterUserId,Status)` + `(Status,CreatedAt)`, Restrict deletes (never cascade-delete reports when post hidden).
- `IPostReportRepository` in Core; `PostReportRepository` in Infrastructure with explicit `!`-filters, `ValidatePage`/`NormalizeLimit` (throw <1, clamp 50), transactions where counters change.
- Application CQRS: `ReportPostCommand` + validator (reason enum: Spam, Harassment, HateSpeech, Nudity, Violence, Misinformation, Copyright, Other; details max 1000, required when reason=Other), `GetMyReportsQuery`, `CancelReportCommand`, `GetReportsQueueQuery`, `GetReportByIdQuery`, `ResolveReportCommand` + validator.
- API: user routes on `PostsController` (`POST {postId}/report`, `GET reports/mine`, `DELETE reports/{id}`); admin routes on `AdminModerationController` (`GET reports`, `GET reports/{id}`, `POST reports/{id}/resolve`). Envelope + error matrix per compass §3/§5.
- Dashboard: extend `AdminDashboardController` with `GET /admin/reports` + `RenderDashboard("reports")` branch, sidebar CONTENT entry, reports panel (filter pills, table, inspect + resolve modals, JS fetch with admin_token cookie). Professional polish: reuse tokens, tabular-nums, responsive.
- Tests: SQLite repo tests (duplicate guard, self-report, status transition) + integration tests (user 400/404, admin queue/resolve/audit, dashboard route 200/redirect).

## Scope IN
Entity, migration (additive), repository, CQRS, user + admin endpoints, dashboard page + nav polish, tests, docs row, SDK regen input (build emits spec).

## Scope OUT
Comment reporting, auto-hide thresholds, email/push delivery, prod `database update`, prod binary deploy, operationIds.

## Dependencies
- `Post`, `User`, `AuditLog`, `Notification` pipelines; `INotificationRepository.AddAsync` (block/self-skip/toggle respected).
- 2 pending prod migrations stay unapplied; new migration also stays unapplied without human approval.

## Complexity
M (new entity + 6 endpoints + dashboard page + tests; no breaking schema).
