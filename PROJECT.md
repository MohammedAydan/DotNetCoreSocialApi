# Project: Embedded Admin Dashboard (DotNetCoreSocialApi)

## Architecture
Clean Architecture with ASP.NET Core 9, EF Core (MySQL / Pomelo), ASP.NET Core Identity, Redis Distributed Cache with In-Memory fallback, and embedded Razor Pages Web Management Interface.

### Layer Responsibilities
- **Domain (`Social.Core`)**:
  - Domain entities: `User`, `Post`, `Comment`, `Media`, `AuditLog`, `RefreshToken`, etc.
  - Interface contracts: `IUserRepository`, `IPostRepository`, `ICommentRepository`, `IAuditLogRepository`, `ISystemMetricsService`, `ICacheService`.
  - Zero external framework or database dependencies.
- **Application (`Social.Application`)**:
  - MediatR commands, queries, and validators for administrative operations (`Features/Admin/*`, `Features/AuditLogs/*`).
  - Validation behaviors (`ValidationBehavior<TRequest, TResponse>`).
  - Strict business invariant rules (no self-banning, no self-demotion, non-negative counter bounds).
- **Infrastructure (`Social.Infrastructure`)**:
  - `ApplicationDbContext` with `AuditLogs` DbSet, migrations, and index definitions.
  - Repository implementations (`AuditLogRepository`, `UserRepository`, `PostRepository`, `CommentRepository`).
  - Diagnostics service querying Redis stats (`INFO stats`), memory fallback check, and rate-limiting store.
- **Presentation (`Social`)**:
  - Embedded Razor Pages under `Pages/Admin/*` (`/admin`, `/admin/login`, `/admin/users`, `/admin/moderation`, `/admin/analytics`, `/admin/audit-logs`).
  - REST API administrative controllers under `Controllers/Admin/*`.
  - Dual-delivery JWT authentication (Header `Authorization: Bearer` and HttpOnly Cookie `admin_token`).
  - `TokenBlacklistMiddleware` with user-level ban cache check (`blacklisted_user:{userId}`).
- **Testing (`Social.Tests`)**:
  - 74 baseline tests preserved with 0 regressions.
  - E2E and integration tests covering 4 tiers (Tiers 1-4) plus adversarial coverage hardening (Tier 5).

---

## Feature Inventory
Every feature from the Survey phase mapped to its assigned milestone:

| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | Embedded Razor Pages Web Interface | Responsive admin UI hosted directly at `/admin/*` | M5 | ORIGINAL_REQUEST §R1 |
| 2 | Strict RBAC Route & Endpoint Protection | 401 Unauthorized for anonymous; 403 Forbidden for non-admin | M1, M5 | ORIGINAL_REQUEST §R1 |
| 3 | Dual-Delivery JWT Bearer & Cookie Auth | Cookie fallback in `JwtBearerEvents.OnMessageReceived` | M1, M5 | survey_explorer_1 |
| 4 | Existing REST API & Swagger Isolation | 0 regressions, `/api/v1/*` and `/swagger` preserved | M1, M5, M6 | ORIGINAL_REQUEST §R1 |
| 5 | Moderator Role Seeding | Ensure `"Moderator"` role exists in Identity seeding | M1 | survey_explorer_2 |
| 6 | Database-Level Paginated User Search | Paginated `IQueryable` query with username/email/role/status filters | M2 | ORIGINAL_REQUEST §R2 |
| 7 | User Account Ban & Lockout | Lock account (`LockoutEnd`) and revoke refresh tokens | M2 | ORIGINAL_REQUEST §R2 |
| 8 | Immediate Token Revocation & Blacklist | Invalidate active tokens via cache key `blacklisted_user:{userId}` | M2 | survey_explorer_1, 2 |
| 9 | User Account Unban | Clear lockout end date and reset failed access counter | M2 | ORIGINAL_REQUEST §R2 |
| 10 | Role Assignment & Revocation | Assign/revoke `Admin`, `Moderator`, `User` with self-demotion guard | M2 | ORIGINAL_REQUEST §R2 |
| 11 | Verified Account Badge Toggle | Toggle `IsVerified` flag on user profile | M2 | ORIGINAL_REQUEST §R2 |
| 12 | Administrative Password Reset | Reset password or generate reset token without user email flow | M2 | ORIGINAL_REQUEST §R2 |
| 13 | Centralized Moderation Feed | Paginated feed of posts, comments, and media assets | M3 | ORIGINAL_REQUEST §R3 |
| 14 | Post Soft-Delete / Hide | Soft-delete post (`IsDeleted = true`) with safe counter decrement | M3 | ORIGINAL_REQUEST §R3 |
| 15 | Post Restore | Restore post (`IsDeleted = false`) with counter increment | M3 | ORIGINAL_REQUEST §R3 |
| 16 | Comment Soft-Delete / Hide | Soft-delete comment (`IsDeleted = true`) with counter decrement | M3 | ORIGINAL_REQUEST §R3 |
| 17 | Comment Restore | Restore comment (`IsDeleted = false`) with counter increment | M3 | ORIGINAL_REQUEST §R3 |
| 18 | Non-Negative Counter Invariant | Protect `PostsCount`, `CommentsCount`, `RepliesCount` (`Math.Max(0, c - 1)`) | M3 | survey_explorer_2, 3 |
| 19 | Real-Time Platform Overview Metrics | Total users, active sessions, posts, comments, engagement totals | M4 | ORIGINAL_REQUEST §R4 |
| 20 | Operational Diagnostics & Cache Observability | Redis connection, fallback status, hit/miss ratio (`INFO stats`) | M4 | ORIGINAL_REQUEST §R4 |
| 21 | Rate-Limiting Activity Monitoring | Tracked IP clients, rules, and throttled request counters | M4 | ORIGINAL_REQUEST §R4 |
| 22 | Audit Log Entity & Repository | Persistent `AuditLog` domain model and `IAuditLogRepository` | M1 | ORIGINAL_REQUEST §R5 |
| 23 | Administrative Mutation Audit Interceptor | Capture admin ID, action type, target entity, timestamp, reason | M1, M2, M3 | ORIGINAL_REQUEST §R5 |
| 24 | Searchable Audit Log Viewer | Descending timeline with action, target, admin, and date filtering | M5 | ORIGINAL_REQUEST §R5 |
| 25 | E2E Testing Suite (Tiers 1-4) | Comprehensive test suite covering features, boundaries, combinations | M6, Test Track | survey_spec_miner_3 |
| 26 | Adversarial Hardening (Tier 5) | Adversarial test cases and stress validation | M6 | Project Pattern |

---

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | Audit Logging & RBAC Foundation | `AuditLog` entity, `IAuditLogRepository`, `ApplicationDbContext` mapping, `AuditLogRepository`, `"Moderator"` role seed, `blacklisted_user:{userId}` check in `TokenBlacklistMiddleware` | none | PLANNED |
| M2 | User & Identity Management | `IUserRepository` admin methods, `Features/Admin/Users/*` commands & queries (search, ban, unban, roles, verify, reset password), audit emission | M1 | PLANNED |
| M3 | Content Moderation & Media Oversight | `IPostRepository`/`ICommentRepository` moderation methods, `Features/Admin/Moderation/*` commands & queries (feed, hide, restore), non-negative counter sync, audit emission | M1 | PLANNED |
| M4 | Platform Analytics & Observability | `ISystemMetricsService`, `Features/Admin/Analytics/*` queries (overview metrics, Redis diagnostics, rate limit metrics, runtime health) | M1 | PLANNED |
| M5 | Embedded Admin Web Interface | Razor Pages under `Pages/Admin/*` (`/admin`, `/admin/login`, `/admin/users`, `/admin/moderation`, `/admin/analytics`, `/admin/audit-logs`), dual-delivery JWT/Cookie auth, strict RBAC (401/403) | M1, M2, M3, M4 | PLANNED |
| M6 | Final Milestone: 100% E2E Pass & Adversarial Hardening | Phase 1: Pass 100% of E2E test suite (Tiers 1-4) published in `TEST_READY.md`. Phase 2: Adversarial coverage hardening (Tier 5) | M5, TEST_READY.md | PLANNED |

In parallel:
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| E2E | E2E Testing Track | Test infrastructure (`TEST_INFRA.md`), test cases across Tiers 1-4, publication of `TEST_READY.md` | none | PLANNED |

---

## Interface Contracts

### Domain & Infrastructure Contracts (`Social.Core/Interfaces`)

#### 1. `IAuditLogRepository`
```csharp
public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log, CancellationToken cancellationToken = default);
    Task<(IEnumerable<AuditLog> Items, int TotalCount)> GetLogsAsync(
        string? actionType = null,
        string? targetEntity = null,
        string? adminId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
}
```

#### 2. Administrative Extensions to `IUserRepository`
```csharp
Task<bool> BanUserAsync(string userId, string? reason, DateTimeOffset? lockoutEnd = null, CancellationToken ct = default);
Task<bool> UnbanUserAsync(string userId, CancellationToken ct = default);
Task<bool> AssignRoleAsync(string userId, string role, CancellationToken ct = default);
Task<bool> RemoveRoleAsync(string userId, string role, CancellationToken ct = default);
Task<bool> SetVerificationStatusAsync(string userId, bool isVerified, CancellationToken ct = default);
Task<bool> AdminResetPasswordAsync(string userId, string newPassword, CancellationToken ct = default);
Task<(IEnumerable<User> Items, int TotalCount)> GetAdminUsersAsync(
    string? query,
    string? role,
    bool? isLocked,
    bool? isVerified,
    int page,
    int pageSize,
    CancellationToken ct = default);
```

#### 3. Moderation Extensions to `IPostRepository` & `ICommentRepository`
```csharp
// IPostRepository
Task<bool> ModeratePostAsync(string postId, bool hide, CancellationToken ct = default);
Task<(IEnumerable<Post> Items, int TotalCount)> GetModerationPostsAsync(
    int page,
    int pageSize,
    bool? isDeleted,
    bool? hasMedia,
    string? search,
    CancellationToken ct = default);

// ICommentRepository
Task<bool> ModerateCommentAsync(string commentId, bool hide, CancellationToken ct = default);
Task<(IEnumerable<Comment> Items, int TotalCount)> GetModerationCommentsAsync(
    int page,
    int pageSize,
    bool? isDeleted,
    string? postId = null,
    CancellationToken ct = default);
```

#### 4. `ISystemMetricsService`
```csharp
public interface ISystemMetricsService
{
    Task<PlatformOverviewMetricsDto> GetOverviewMetricsAsync(CancellationToken ct = default);
    Task<SystemDiagnosticsDto> GetDiagnosticsAsync(CancellationToken ct = default);
}
```

---

## Code Layout
- `Social.Core/Entities/`: `AuditLog.cs`
- `Social.Core/Interfaces/`: `IAuditLogRepository.cs`, `ISystemMetricsService.cs`
- `Social.Application/Features/Admin/`:
  - `Users/` (Commands, Queries, DTOs, Validators)
  - `Moderation/` (Commands, Queries, DTOs, Validators)
  - `Analytics/` (Queries, DTOs)
  - `AuditLogs/` (Queries, DTOs)
  - `Common/` (Admin audit recording helper / pipeline behavior)
- `Social.Infrastructure/Data/`: `ApplicationDbContext.cs` (DbSet<AuditLog>)
- `Social.Infrastructure/Repositories/`: `AuditLogRepository.cs`, updates to `UserRepository.cs`, `PostRepository.cs`, `CommentRepository.cs`
- `Social.Infrastructure/Diagnostics/`: `SystemMetricsService.cs`
- `Social/Pages/Admin/`:
  - `Index.cshtml` / `Index.cshtml.cs` (Overview & Analytics)
  - `Login.cshtml` / `Login.cshtml.cs`
  - `Users.cshtml` / `Users.cshtml.cs`
  - `Moderation.cshtml` / `Moderation.cshtml.cs`
  - `AuditLogs.cshtml` / `AuditLogs.cshtml.cs`
  - `_Layout.cshtml`, `_ViewStart.cshtml`, `_ViewImports.cshtml`
- `Social/Controllers/Admin/`: Admin REST API controllers mirroring page operations
- `Social/Middlewares/`: `TokenBlacklistMiddleware.cs` (enhancement for user blacklist)
- `Social.Tests/`:
  - `Unit/Features/Admin/*`
  - `Integration/Admin/*`
  - `Infrastructure/CustomWebApplicationFactory.cs` (extended with administrative mock substitutes)
