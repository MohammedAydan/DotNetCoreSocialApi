# Feature Plan: Dashboard Dedicated Routes & EF Core Fixes

## Goal
Resolve EF Core set operation translation failure and missing `AuditLogs` database table errors, implement dedicated browser routes for each dashboard module (`/admin`, `/admin/users`, `/admin/moderation`, `/admin/audit-logs`, `/admin/diagnostics`), and enhance UI/UX with professional dark-slate design and rich feature set.

## Acceptance Criteria
1. **EF Core Translation Fix:** `AdminRepository.GetModerationFeedAsync` translates and runs successfully on MySQL without set operation projection exceptions.
2. **AuditLogs Table Migration & Resilience:**
   - Create EF Core migration `AddAuditLogsTable`.
   - `DatabaseSeeder.SeedAsync` executes `context.Database.MigrateAsync()` on startup (non-testing) to apply pending migrations automatically.
   - `AuditLogRepository` handles unmigrated/missing table states gracefully with logging rather than throwing unhandled 500 errors.
3. **Dedicated Admin Browser Routes:**
   - `GET /admin` -> Overview & Real-Time Analytics
   - `GET /admin/users` -> User & Identity Directory
   - `GET /admin/moderation` -> Content Moderation Feed
   - `GET /admin/audit-logs` -> Administrative Audit Trail
   - `GET /admin/diagnostics` -> System Diagnostics & Cache Health
   - `GET /admin/login` -> Dedicated Login Portal
   - `GET/POST /admin/logout` -> Session Sign Out
4. **Enhanced UI/UX:**
   - Real URL navigation with browser history, bookmarks, and deep links.
   - Active sidebar indicator matching the active route.
   - Modern dark slate theme, toast feedback for actions, and interactive action modals.
5. **Test Integrity:** All 130 unit and integration tests pass with 0 failures (`dotnet test Social.sln -c Release`).
6. **Clean Build:** 0 compiler errors in Release mode.

## Complexity
Medium (M)
