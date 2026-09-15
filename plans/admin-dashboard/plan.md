# Admin Dashboard Plan

## Goal
Build an embedded, production-grade Admin Dashboard and management subsystem directly within the ASP.NET Core 9 solution for comprehensive management, moderation, and operational observability of the social media platform, while preserving all existing Clean Architecture boundaries and passing test suites.

## Acceptance Criteria
1. **Security & Access Control (RBAC):**
   - Routes under `/admin` and `/api/admin/*` require `Admin` role (except moderation feed/actions which allow `Admin` or `Moderator`).
   - Anonymous requests return HTTP 401 or redirect to login.
   - Unauthorized roles (regular `User`, or `Moderator` attempting user management) return HTTP 403 Forbidden.
   - Non-admin endpoints (`/api/v1/*`, `/api/*`) continue to function without regressions.
2. **User Management:**
   - Query users with pagination, clamping, and case-insensitive/sanitized search (`GET /api/admin/users`).
   - Ban user with duration & reason, revoking active tokens and preventing login (`POST /api/admin/users/{id}/ban`).
   - Self-ban invariant protection (admin cannot ban themselves -> HTTP 400).
   - Unban user (`POST /api/admin/users/{id}/unban`), rejecting unban if not locked (HTTP 400).
   - Assign/update roles (`POST /api/admin/users/{id}/roles`), preventing self-demotion from Admin (HTTP 400) and rejecting invalid role names (HTTP 400).
   - Toggle verification badge (`POST /api/admin/users/{id}/verify`).
   - Administrative password reset (`POST /api/admin/users/{id}/reset-password`) with password complexity validation.
3. **Content Moderation:**
   - Moderation feed listing posts & comments (`GET /api/admin/moderation/feed`).
   - Soft-delete/hide post (`POST /api/admin/moderation/posts/{id}/hide`) and restore (`POST /api/admin/moderation/posts/{id}/restore`). Safe counter decrements (preventing negative values). Double-hide or restoring an active post returns HTTP 400.
   - Soft-delete/hide comment (`POST /api/admin/moderation/comments/{id}/hide`) and restore (`POST /api/admin/moderation/comments/{id}/restore`). Safe comment counter decrements.
4. **Analytics & Diagnostics:**
   - Overview metrics (`GET /api/admin/analytics/overview`): Total users, 24h active users, total posts, comments, likes.
   - Diagnostics (`GET /api/admin/analytics/diagnostics`): Cache status (connected, memory fallback), rate-limiting stats, memory working set, environment.
5. **Audit Logging:**
   - Persistent `AuditLog` entity recording admin mutations (admin ID, target ID, target type, action type, reason, timestamp).
   - Searchable, paginated audit log endpoint (`GET /api/admin/audit-logs`).
6. **Embedded Web UI:**
   - Responsive web dashboard hosted under `/admin` with tabs for Overview, Users, Moderation, and Audit Logs.
7. **Testing & Verification:**
   - All 74 existing baseline tests in `Social.Tests` continue to pass.
   - All 47 integration tests in `Social.Tests/Integration/Admin/` pass 100%.
   - Release build succeeds with 0 errors.

## Approach
- **Core Layer (`Social.Core`):**
  - Define `AuditLog` entity in `Social.Core.Entities`.
  - Define `IAuditLogRepository` interface in `Social.Core.Interfaces`.
  - Define `IAdminRepository` (or extend repositories) with cancellation tokens and pagination contracts.
- **Application Layer (`Social.Application`):**
  - Implement CQRS Commands, Queries, Handlers, and DTOs under `Social.Application.Features.Admin`.
  - Add FluentValidation rules for admin requests.
- **Infrastructure Layer (`Social.Infrastructure`):**
  - Configure EF Core mappings for `AuditLog` in `ApplicationDbContext`.
  - Implement `AuditLogRepository` and admin data access.
  - Implement `Moderator` role seeding in `EnsureRolesExistAsync`.
  - Implement instant token invalidation and lockout integration.
- **API Host Layer (`Social`):**
  - Authorize policies for `AdminOnly` and `AdminOrModerator`.
  - Implement REST controllers: `AdminUsersController`, `AdminModerationController`, `AdminAnalyticsController`, `AdminAuditLogsController`.
  - Host the embedded `/admin` web interface using Razor Pages / Minimal HTML UI with full Admin authorization.
- **Test Doubles (`Social.Tests`):**
  - Wire `CustomWebApplicationFactory` with `IAuditLogRepository` and admin repository mocks/in-memory test doubles as required to satisfy Tier 1 - Tier 4 tests.

## Scope
- In Scope: Full backend administration API, embedded UI, audit logging, security invariants, automated tests.
- Out of Scope: Third-party admin SaaS integrations, standalone SPA frontend build steps.

## Complexity
Large (L)
