# Tasks: Dashboard Dedicated Routes & EF Core Fixes

- [x] Task 1: Fix EF Core Translation in `AdminRepository.GetModerationFeedAsync`
  - [x] Refactor `GetModerationFeedAsync` to avoid client projection set operation collision
- [x] Task 2: Database Migration & Resilience for `AuditLogs`
  - [x] Create EF Core migration `AddAuditLogsTable`
  - [x] Add `context.Database.MigrateAsync()` in `DatabaseSeeder.SeedAsync`
  - [x] Add exception resilience in `AuditLogRepository` to prevent 500 crashes if table is temporarily unavailable
- [x] Task 3: Dedicated Routes & Enhanced UI in `AdminDashboardController`
  - [x] Implement dedicated routes: `/admin`, `/admin/users`, `/admin/moderation`, `/admin/audit-logs`, `/admin/diagnostics`
  - [x] Update HTML shell generation to support active page context, route links, and history
  - [x] Enhance UI with toast feedback, refined dark styling, and modal operations
- [x] Task 4: Verification & Tests
  - [x] Add route tests verifying `/admin/users`, `/admin/moderation`, `/admin/audit-logs`, `/admin/diagnostics`
  - [x] Run full test suite (`dotnet test Social.sln -c Release`)
  - [x] Verify 0 compiler errors (`dotnet build Social.sln -c Release`)
- [x] Task 5: Documentation & Closure
  - [x] Write `plans/dashboard-routes-and-fix/review.md`
  - [x] Update `plans/context.md` and `plans/SESSION_LOG.md`
