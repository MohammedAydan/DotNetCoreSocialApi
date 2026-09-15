# Feature Review: Admin Dashboard

## What Was Built
1. **Domain & Storage Layer (`Social.Core`, `Social.Infrastructure`):**
   - Added `AuditLog` domain entity with tracking for admin ID, email, action type, target entity, target ID, reason, and UTC timestamp.
   - Added `IAuditLogRepository` and `IAdminRepository` domain abstractions with full `CancellationToken` support.
   - Configured EF Core `DbSet<AuditLog>` mapping in `ApplicationDbContext`.
   - Implemented `AuditLogRepository` and `AdminRepository` in `Social.Infrastructure/Repositories/`.
   - Added `Moderator` role seeding to `UserRepository.EnsureRolesExistAsync()`.
   - Registered all repositories in DI pipeline.

2. **Application Layer (`Social.Application`):**
   - Standardized DTOs under `Social.Application.Features.Admin.DTOs`.
   - Implemented CQRS commands & handlers with strict domain invariants:
     - `BanUserCommand` (with self-ban guard and token invalidation).
     - `UnbanUserCommand` (with lockout check).
     - `UpdateUserRolesCommand` (with self-demotion guard and role whitelist validation).
     - `ToggleUserVerificationCommand`.
     - `AdminResetPasswordCommand` (with password complexity validation).
     - `HidePostCommand` / `RestorePostCommand` (with safe counter decrements and status guards).
     - `HideCommentCommand` / `RestoreCommentCommand` (with safe counter decrements).
   - Implemented CQRS queries & handlers:
     - `GetAdminUsersQuery` (with pagination clamping and safe sanitized search).
     - `GetModerationFeedQuery` (combined posts and comments feed).
     - `GetPlatformOverviewQuery` (total and active 24h platform metrics).
     - `GetSystemDiagnosticsQuery` (process memory working set, cache state, environment).
     - `GetAuditLogsQuery` (with date range validation and filtering).

3. **Presentation & UI (`Social.Admin.Web` & `Social.API`):**
   - **Dedicated UI Sub-Project (`Social.Admin.Web`)**:
     - Standalone Razor Class Library (`Microsoft.NET.Sdk.Razor` targeting `net9.0`).
     - Layout components: `AdminLayout.razor`, `AdminSidebar.razor`, `AdminNavbar.razor`, `AdminFooter.razor`.
     - Common reusable components: `MetricCard.razor`, `StatusBadge.razor`, `ConfirmModal.razor`, `PaginationControl.razor`.
     - Interactive page components: `OverviewDashboard.razor`, `UserManagement.razor`, `ContentModeration.razor`, `AuditLogViewer.razor`, `SystemDiagnostics.razor`.
     - Action modal components: `BanUserModal.razor`, `UnbanUserModal.razor`, `RoleManagerModal.razor`, `ResetPasswordModal.razor`, `ModerationActionModal.razor`.
     - UI Models & Orchestrating Service: `IAdminDashboardService` and `AdminDashboardService`.
     - Modern slate theme assets: `admin-dashboard.css` and `admin-dashboard.js`.
   - **REST Controllers & UI Host (`Social.API`)**:
     - `AdminUsersController` (`/api/admin/users`) with `[Authorize(Roles = "Admin")]`.
     - `AdminModerationController` (`/api/admin/moderation`) with `[Authorize(Roles = "Admin,Moderator")]`.
     - `AdminAnalyticsController` (`/api/admin/analytics`) with `[Authorize(Roles = "Admin,Moderator")]`.
     - `AdminAuditLogsController` (`/api/admin/audit-logs`) with `[Authorize(Roles = "Admin")]`.
     - `AdminDashboardController` (`/admin`) hosting the rich UI shell powered by `Social.Admin.Web` with strict `[Authorize(Roles = "Admin")]` enforcement.

4. **Testing & Verification (`Social.Tests`):**
   - Added `TestAdminRepository` and `TestAuditLogRepository` test doubles in `Social.Tests/Infrastructure/`.
   - Wired `CustomWebApplicationFactory` to support test doubles and delegated post lookup.
   - All 47 Admin tests across Tier 1 (Feature Coverage), Tier 2 (Boundaries & Corners), Tier 3 (Cross-Feature Workflows), and Tier 4 (Real-World Scenarios) pass 100%.
   - All 74 existing baseline unit and integration tests pass 100%. Total: **121 / 121 tests passing**.
   - Release build compiles with 0 errors.

## Edge Cases Handled
- Self-ban protection: Administrators attempting to ban their own account receive HTTP 400 BadRequest.
- Self-demotion protection: Administrators attempting to revoke their own `Admin` role receive HTTP 400 BadRequest.
- Counter bounds: Decrementing post or comment counters upon soft deletion uses safe math (`Math.Max(0, count - 1)`), preventing negative counters.
- Date range validation: Reversing `fromDate` and `toDate` in audit log queries returns HTTP 400 BadRequest.
- Pagination bounds: Negative page numbers are clamped to 1, and excessive page sizes are clamped to 100.
- Password complexity: Administrative password resets enforce character length, uppercase, and digit rules.
