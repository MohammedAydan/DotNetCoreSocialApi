# Context: Dashboard Dedicated Routes & EF Core Fixes

## Problem Statement
1. `AdminRepository.GetModerationFeedAsync` failed with EF Core set operation translation error when calling `CountAsync` on `postsQuery.Concat(commentsQuery)` after client projections.
2. `AuditLogRepository.GetPagedAsync` failed because table `AuditLogs` was never migrated in MySQL.
3. `/admin` had client-side DOM tab switching instead of actual dedicated browser routes for `/admin/users`, `/admin/moderation`, `/admin/audit-logs`, `/admin/diagnostics`.

## Files to Modify / Create
- `Social.Infrastructure/Repositories/AdminRepository.cs` (fix moderation feed query)
- `Social.Infrastructure/Repositories/AuditLogRepository.cs` (add resilience against missing table)
- `Social.Infrastructure/Services/DatabaseSeeder.cs` (apply pending migrations on startup)
- `Social.Infrastructure/Migrations/*` (add `AddAuditLogsTable` migration)
- `Social/Controllers/Admin/AdminDashboardController.cs` (add dedicated route actions & UI enhancements)
- `Social.Tests/Integration/Admin/AdminAuthenticationTests.cs` (verify dedicated routes)

## Dependencies Added
- None.
