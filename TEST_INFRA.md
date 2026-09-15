# Test Infrastructure & Strategy Specification: Admin Dashboard (DotNetCoreSocialApi)

## 1. Executive Summary

This document establishes the comprehensive test architecture, feature inventory mapping, 4-tier testing hierarchy, and execution guidelines for the embedded **Admin Dashboard** in `DotNetCoreSocialApi` (.NET 9).

The solution adheres to Clean Architecture with ASP.NET Core 9, Pomelo MySQL EF Core, ASP.NET Core Identity, Redis Distributed Caching with in-memory fallback, and embedded Razor Pages management views. The test infrastructure leverages **xUnit**, **FluentAssertions**, **NSubstitute**, and **Microsoft.AspNetCore.Mvc.Testing** (`WebApplicationFactory<Program>`) to execute opaque-box, end-to-end (E2E) integration tests directly against the ASP.NET Core HTTP request pipeline.

---

## 2. Feature Inventory Coverage Mapping

Every administrative requirement and architectural feature defined in `PROJECT.md` and `ORIGINAL_REQUEST.md` is covered across the 4-tier test architecture:

| # | Feature | Target Milestone | Primary Test Tier | Test Method / Scope |
|---|---------|------------------|-------------------|---------------------|
| 1 | Embedded Razor Pages Web Interface (`/admin/*`) | M5 | Tier 1, Tier 4 | `T1_AdminDashboard_GetRoot_ReturnsSuccess`<br/>`T4_AbusiveContentIncident_FullLifecycle` |
| 2 | Strict RBAC Protection (401/403) | M1, M5 | Tier 1, Tier 2 | `T1_AdminEndpoints_Anonymous_ReturnsUnauthorized`<br/>`T1_AdminEndpoints_UserRole_ReturnsForbidden`<br/>`T1_AdminEndpoints_ModeratorRole_OnAdminOnly_ReturnsForbidden`<br/>`T1_AdminEndpoints_AdminRole_ReturnsSuccess` |
| 3 | Dual-Delivery JWT Bearer & Cookie Auth | M1, M5 | Tier 1, Tier 3 | `T1_DashboardSignIn_ValidAdmin_ReturnsToken`<br/>`T3_RolePromotion_EnablesImmediateDashboardAccess` |
| 4 | Existing REST API & Swagger Isolation | M1, M5, M6 | Baseline | Existing 74 tests in `Social.Tests` (100% non-regression) |
| 5 | Moderator Role Seeding | M1 | Tier 1 | `T1_AdminEndpoints_ModeratorRole_OnModeration_ReturnsSuccess` |
| 6 | Database-Level Paginated User Search | M2 | Tier 1, Tier 2 | `T1_GetUsers_WithPagination_ReturnsUsersList`<br/>`T2_Pagination_ClampsNegativePageAndExcessiveLimit`<br/>`T2_UserSearch_HandlesEmptyQueryGracefully`<br/>`T2_UserSearch_SanitizesSqlWildcards` |
| 7 | User Account Ban & Lockout | M2 | Tier 1, Tier 2, Tier 3 | `T1_BanUser_ValidTarget_ReturnsSuccess`<br/>`T2_BanUser_WhenTargetNotFound_ReturnsNotFound`<br/>`T2_BanUser_WhenTargetIsSelf_ReturnsBadRequest`<br/>`T3_BanUser_PreventsSubsequentSignIn` |
| 8 | Immediate Token Revocation & Blacklist | M2 | Tier 3 | `T3_BanUser_InvalidatesExistingTokens` |
| 9 | User Account Unban | M2 | Tier 1, Tier 2 | `T1_UnbanUser_ValidTarget_ReturnsSuccess`<br/>`T2_UnbanUser_WhenUserNotLocked_ReturnsBadRequest` |
| 10 | Role Assignment & Revocation | M2 | Tier 1, Tier 2, Tier 3 | `T1_UpdateRoles_ValidTarget_ReturnsUpdatedRoles`<br/>`T2_RoleChange_WhenDemotingSelf_ReturnsBadRequest`<br/>`T2_RoleChange_WithInvalidRoleName_ReturnsBadRequest`<br/>`T3_RolePromotion_EnablesImmediateDashboardAccess` |
| 11 | Verified Account Badge Toggle | M2 | Tier 1 | `T1_ToggleVerification_ValidTarget_ReturnsSuccess` |
| 12 | Administrative Password Reset | M2 | Tier 1, Tier 2 | `T1_ResetPassword_ValidTarget_ReturnsSuccess`<br/>`T2_ResetPassword_WeakPassword_ReturnsBadRequest` |
| 13 | Centralized Moderation Feed | M3 | Tier 1 | `T1_ModerationFeed_ReturnsAllItems` |
| 14 | Post Soft-Delete / Hide | M3 | Tier 1, Tier 2, Tier 3 | `T1_HidePost_ValidId_SetsIsDeletedTrue`<br/>`T2_HidePost_WhenCountIsZero_DoesNotBecomeNegative`<br/>`T2_HidePost_WhenAlreadyHidden_ReturnsBadRequest`<br/>`T3_Moderation_HidePost_ExcludesFromPublicFeeds` |
| 15 | Post Restore | M3 | Tier 1, Tier 2, Tier 3 | `T1_RestorePost_ValidId_SetsIsDeletedFalse`<br/>`T2_RestorePost_WhenNotHidden_ReturnsBadRequest`<br/>`T3_Moderation_HidePost_ExcludesFromPublicFeeds` |
| 16 | Comment Soft-Delete / Hide | M3 | Tier 1, Tier 2, Tier 3 | `T1_HideComment_ValidId_SetsIsDeletedTrue`<br/>`T2_HideComment_WhenCountIsZero_DoesNotBecomeNegative`<br/>`T2_HideComment_WhenAlreadyHidden_ReturnsBadRequest`<br/>`T3_Moderation_HideComment_SynchronizesPostCommentCounter` |
| 17 | Comment Restore | M3 | Tier 1, Tier 2, Tier 3 | `T1_RestoreComment_ValidId_SetsIsDeletedFalse`<br/>`T2_RestoreComment_WhenNotHidden_ReturnsBadRequest`<br/>`T3_Moderation_HideComment_SynchronizesPostCommentCounter` |
| 18 | Non-Negative Counter Invariant | M3 | Tier 2 | `T2_HidePost_WhenCountIsZero_DoesNotBecomeNegative`<br/>`T2_HideComment_WhenCountIsZero_DoesNotBecomeNegative` |
| 19 | Real-Time Platform Overview Metrics | M4 | Tier 1, Tier 4 | `T1_GetOverviewMetrics_ReturnsPlatformCounts`<br/>`T4_AbusiveContentIncident_FullLifecycle` |
| 20 | Operational Diagnostics & Cache Observability | M4 | Tier 1, Tier 2, Tier 4 | `T1_GetDiagnostics_ReturnsSystemHealth`<br/>`T2_Diagnostics_WhenRedisDisconnected_ReportsFallbackActive`<br/>`T4_PlatformObservabilityUnderLoad` |
| 21 | Rate-Limiting Activity Monitoring | M4 | Tier 1, Tier 4 | `T1_GetDiagnostics_ReturnsSystemHealth`<br/>`T4_PlatformObservabilityUnderLoad` |
| 22 | Audit Log Entity & Repository | M1 | Tier 1, Tier 3 | `T1_GetAuditLogs_ReturnsOrderedRecords`<br/>`T3_AdministrativeMutation_GeneratesPersistentAuditRecord` |
| 23 | Administrative Mutation Audit Interceptor | M1, M2, M3 | Tier 3, Tier 4 | `T3_AdministrativeMutation_GeneratesPersistentAuditRecord`<br/>`T4_AbusiveContentIncident_FullLifecycle`<br/>`T4_RoleDelegationAndAuditReview` |
| 24 | Searchable Audit Log Viewer | M5 | Tier 1, Tier 2 | `T1_GetAuditLogs_ReturnsOrderedRecords`<br/>`T2_AuditLogQuery_NonExistentFilter_ReturnsEmptyList`<br/>`T2_AuditLogQuery_InvalidDateRange_ReturnsBadRequest` |
| 25 | E2E Testing Suite (Tiers 1-4) | M6, E2E Track | All Tiers | Full integration test suite in `Social.Tests/Integration/Admin/*` |
| 26 | Adversarial Hardening (Tier 5) | M6 | Tier 5 | Stress, security headers, fuzzing (to be executed in M6) |

---

## 3. 4-Tier Test Architecture

```
                    ┌─────────────────────────────────────────────────────────┐
                    │       Tier 4: Real-World Scenarios (Lifecycle E2E)      │
                    │       - Abusive content triage & account lockout        │
                    │       - Role delegation, moderation & audit review      │
                    │       - Platform observability & rate-limit diagnostics │
                    ├─────────────────────────────────────────────────────────┤
                    │    Tier 3: Cross-Feature Integration Workflows          │
                    │    - Ban -> Instant sign-in rejection & token revoke    │
                    │    - Moderation hide -> Public feed omission            │
                    │    - Comment hide/restore -> Counter synchronization    │
                    │    - Mutation -> Persistent audit log verification      │
                    ├─────────────────────────────────────────────────────────┤
                    │     Tier 2: Boundary, Corner & Error Cases              │
                    │     - Self-ban & self-demotion prevention (400)         │
                    │     - Non-negative post & comment counters bound        │
                    │     - Pagination clamping & SQL wildcard sanitization   │
                    │     - Redis fallback detection & empty log queries      │
                    ├─────────────────────────────────────────────────────────┤
                    │      Tier 1: Feature Coverage (Baseline Functionality)  │
                    │      - Strict RBAC: 401 Anonymous, 403 User, 200 Admin  │
                    │      - User management CRUD (search, ban, roles, etc.)  │
                    │      - Content moderation feed, hide & restore          │
                    │      - Analytics overview, diagnostics & audit querying │
                    └─────────────────────────────────────────────────────────┘
```

### Tier 1: Feature Coverage
- **Objective**: Verify that every administrative endpoint and web route functions correctly under standard, valid inputs.
- **Coverage**:
  - **RBAC**: Anonymous requests to `/admin` and `/api/admin/*` return HTTP 401; standard `User` role returns HTTP 403; `Admin` role returns HTTP 200.
  - **Auth**: Existing `/api/dashboard/User/sign-in` endpoint succeeds for administrators and denies non-admins.
  - **User Management**: `GET /api/admin/users`, `POST /api/admin/users/{id}/ban`, `POST /api/admin/users/{id}/unban`, `POST /api/admin/users/{id}/roles`, `POST /api/admin/users/{id}/verify`, `POST /api/admin/users/{id}/reset-password`.
  - **Moderation**: `GET /api/admin/moderation/feed`, `POST /api/admin/moderation/posts/{id}/hide`, `POST /api/admin/moderation/posts/{id}/restore`, `POST /api/admin/moderation/comments/{id}/hide`, `POST /api/admin/moderation/comments/{id}/restore`.
  - **Analytics & Observability**: `GET /api/admin/analytics/overview`, `GET /api/admin/analytics/diagnostics`.
  - **Audit Logging**: `GET /api/admin/audit-logs` returning descending sorted logs.

### Tier 2: Boundary, Corner & Error Cases
- **Objective**: Harden the application against invalid arguments, boundary conditions, invariant violations, and edge behaviors.
- **Coverage**:
  - **Self-Harm Protection**: Admins cannot ban their own account (`HTTP 400 BadRequest`) and cannot demote themselves from `Admin` (`HTTP 400 BadRequest`).
  - **Non-Negative Bounds**: Hiding a post when `PostsCount == 0` ensures `PostsCount` remains 0 (never -1). Hiding a comment when `CommentsCount == 0` ensures `CommentsCount` remains 0.
  - **Idempotency & State Errors**: Hiding an already hidden post returns HTTP 400; restoring an active post returns HTTP 400.
  - **Entity Not Found**: Banning or moderating non-existent entities returns HTTP 404.
  - **Pagination Clamping**: `page = -1` and `pageSize = 1000` clamp to valid ranges (page 1, pageSize 100).
  - **Search Query Resilience**: Blank search strings return valid empty or baseline results without throwing; SQL wildcards (`%`, `_`) and quotes are safely handled.
  - **Diagnostics Fallback**: When Redis is unreachable, diagnostics accurately report `UsingMemoryFallback = true` with 200 OK.

### Tier 3: Cross-Feature Integration Workflows
- **Objective**: Verify that actions executed in one module trigger expected, synchronized state changes across dependent platform modules.
- **Coverage**:
  - **Ban -> Sign-In Lockout**: Banning a user immediately prevents subsequent `POST /api/User/sign-in` with an account locked response.
  - **Ban -> Token Revocation**: Banning a user revokes refresh tokens and rejects `POST /api/User/refresh-token`.
  - **Hide Post -> Feed Omission**: Soft-deleting a post immediately removes it from public `/api/posts/feed` and `/api/posts/{id}`; restoring the post brings it back to the feed.
  - **Hide Comment -> Counter Sync**: Soft-deleting a comment decrements the parent post's `CommentsCount`; restoring increments it.
  - **Admin Mutation -> Audit Persistence**: Any administrative mutation immediately generates an audit log record with `AdminId`, `ActionType`, `TargetId`, and `Reason`.
  - **Role Promotion -> Privilege Activation**: Promoting a `User` to `Admin` immediately allows access to `/admin` without 403 Forbidden.

### Tier 4: Real-World Scenarios
- **Objective**: Validate complete operational workflows modeling real-world incident response and platform administration.
- **Coverage**:
  - **Scenario 4.1 (Abusive Content Incident Response)**:
    1. Inspection of moderation feed.
    2. Soft-delete of abusive post with reason.
    3. Navigation to offending author profile and application of account ban.
    4. Verification of chronological audit log entries for both actions.
    5. Verification that platform overview analytics adjust counts accordingly.
  - **Scenario 4.2 (Role Delegation & Audit Review)**:
    1. Primary admin delegates `Moderator` role to a user.
    2. Moderator hides an offensive comment.
    3. Primary admin inspects audit logs filtered by the moderator's user ID.
    4. Primary admin revokes moderator privileges.
    5. Demoted user is immediately rejected with 403 Forbidden on administrative endpoints.
  - **Scenario 4.3 (Platform Observability Under Load)**:
    1. Simulated elevated request rate.
    2. Admin queries operational diagnostics.
    3. Verification of tracked IP counters, rate-limit policies, GC memory, and thread metrics without system failure.

---

## 4. Test Fixtures & Environment Configuration

### 4.1 `CustomWebApplicationFactory`
Located at `Social.Tests/Infrastructure/CustomWebApplicationFactory.cs`:
- Sets environment to `"Testing"`.
- Injects in-memory rate-limiting configurations with high thresholds (`10000`) to prevent false-positive HTTP 429 errors during test runs.
- Replaces database and external dependencies with NSubstitute test doubles in the DI container.
- Replaces default JWT authentication with `TestScheme` via `TestAuthHandler`.

### 4.2 `TestAuthHandler` Authentication Simulation
Integration tests simulate any identity and authorization state via request headers:
- **Anonymous**: `X-Anonymous: true` -> returns HTTP 401 on protected routes.
- **Standard User**: Default behavior or `X-Test-Role: User` -> returns HTTP 403 on admin-only routes.
- **Moderator**: `X-Test-Role: Moderator` -> permitted on moderation endpoints, denied on user management.
- **Administrator**: `X-Test-Role: Admin` -> permitted on all admin routes.
- **Specific User ID**: `X-Test-UserId: <guid>` -> captures the admin or user actor ID in audit trails.

### 4.3 Client Helper Extensions
`TestClientExtensions` simplifies instantiating typed HTTP clients:
- `factory.CreateAdminClient(userId)`
- `factory.CreateModeratorClient(userId)`
- `factory.CreateUserClient(userId)`
- `factory.CreateAnonymousClient()`

---

## 5. Pass Criteria & Quality Gates

1. **Baseline Preservation**: All existing 74 unit and integration tests must pass with 0 regressions (`dotnet test --filter "Category!=AdminE2E"`).
2. **Compilation Integrity**: The entire solution must compile cleanly with 0 errors in both Debug and Release configurations (`dotnet build Social.sln -c Release`).
3. **Admin E2E Verification**: The full administrative test suite across Tiers 1-4 must achieve a 100% pass rate upon completion of milestone implementation (`dotnet test --filter "Category=AdminE2E"`).
4. **Opaque-Box Standard**: Tests verify observable HTTP outcomes (status codes, response bodies, headers, database-state reflections) without relying on internal private state or facade shortcuts.

---

## 6. Test Execution Commands

| Test Scope | Command Line |
|---|---|
| **All Baseline Tests (74 tests)** | `dotnet test --filter "Category!=AdminE2E"` |
| **All Admin E2E Tests (Tiers 1-4)** | `dotnet test --filter "Category=AdminE2E"` |
| **Tier 1 Only (Feature Coverage)** | `dotnet test --filter "Tier=Tier1"` |
| **Tier 2 Only (Boundary & Invariants)** | `dotnet test --filter "Tier=Tier2"` |
| **Tier 3 Only (Cross-Feature Workflows)**| `dotnet test --filter "Tier=Tier3"` |
| **Tier 4 Only (Real-World Scenarios)** | `dotnet test --filter "Tier=Tier4"` |
| **Full Solution Test Suite** | `dotnet test Social.sln` |
