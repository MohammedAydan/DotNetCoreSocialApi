# Architecture — DotNetCoreSocialApi

> Sources: `Social.Infrastructure/Data/ApplicationDbContext.cs`, `Social.Infrastructure/Repositories/PostRepository.cs`, ADRs 001–013 (`plans/DECISIONS.md`). Line references below point at those two files.

## 1. System overview

Clean Architecture, .NET 9 / C# 13, single solution (`Social.sln`):

```
┌─────────────┐     ┌──────────────────┐     ┌─────────────────────────┐     ┌───────────┐
│ Social.Core │◄────│ Social.Applic.   │◄────│ Social.Infrastructure   │◄────│ Social    │
│ Domain +    │     │ CQRS (MediatR),  │     │ EF Core 9 (MySQL/Pomelo)│     │ API       │
│ interfaces, │     │ validators,      │     │ repos, telemetry,       │     │ controll. │
│ entities    │     │ handlers, DTOs   │     │ caching, workers        │     │ middleware│
└─────────────┘     └──────────────────┘     └─────────────────────────┘     └───────────┘
        ▲                    │                         │                        │
        └────────────────────┴── dependency direction ─┴────────────────────────┘
              (Core has zero framework deps: ADR-001; Application→Infrastructure
               reference removed: ADR-002; cache abstraction lives in Core: ADR-003)
```

- **Database:** MySQL 8 via Pomelo EF Core. UTC everywhere (ValueConverter) with microsecond precision (`SetPrecision(6)` — `ApplicationDbContext.cs:429,434`).
- **Caching:** Redis with in-memory fallback behind `Social.Core.Interfaces.ICacheService`.
- **Auth:** JWT Bearer + dual-delivery `admin_token` cookie for the `/admin` browser console (ADR-005). Token blacklist middleware also rejects `blacklisted_user:{sub}` (ADR-009).
- **Admin UI:** `Social.Admin.Web` Razor Class Library served under `/admin` (`[Authorize(Roles="Admin")]`, ADR-004); the analytics dashboard shell upgraded in place is the string-HTML `AdminDashboardController`, the Blazor tree is unwired dead code (ADR-011).
- **Telemetry:** `RequestTelemetryMiddleware` → bounded `Channel<RequestLog>` (10k, drop-on-full) → `RequestLogFlushWorker` (5 s batch); `MetricsAggregationWorker` (00:05 UTC daily, idempotent upsert, 30-day startup backfill). `AuditLog` is the separate admin-action trail and was deliberately left untouched (ADR-011).

## 2. Core architectural invariants (Posts domain)

### 2.1 Soft delete & timeline integrity

- Deleting a post **never removes the row**. `DeletePostAsync` (`PostRepository.cs:131-161`) runs in a transaction and issues:
  ```csharp
  .SetProperty(p => p.IsDeleted, true).SetProperty(p => p.UpdatedAt, now)   // :143-147
  ```
  then decrements the owner's counter with a floor:
  ```csharp
  .SetProperty(u => u.PostsCount, u => u.PostsCount > 0 ? u.PostsCount - 1 : 0) // :152-154
  ```
  `if (postRows == 0) return false` (`:149-150`); a zero-row user counter update throws `KeyNotFoundException` (`:156-157`, race guard).
- Child shares are **never deleted or orphaned**: `ParentPostId` is kept, so share cards survive the parent's deletion.
- `MaskDeletedParentPosts` (`:441-463`) walks the **entire** ancestor chain (`while (current is not null)`) in memory (queries are `AsNoTracking`, so this mutates DTOs, not the DB):
  - For each deleted ancestor it keeps only `Id, UserId, IsDeleted`.
  - Owner-of-that-parent (`viewerUserId == current.UserId`) still sees the raw content.
  - Everyone else gets `Content = "[This content has been deleted]"`, `Title = null`, `Media = new List<Media>()`.
  - Applied chain-wide on all four post read paths (feed / my / single / profile). There is intentionally **no global query filter** for `IsDeleted`, so admin moderation can still list hidden posts (ADR-006).

### 2.2 Strict privacy & sharing rules

`SharePostAsync` (`:46-120`, transaction at `:57`) enforces, in order:

| # | Check (`PostRepository.cs`) | Failure |
|---|-----------------------------|---------|
| 1 | `originalPost is null \|\| originalPost.IsDeleted` (`:71-72`) | `KeyNotFoundException("Target post does not exist or has been deleted.")` |
| 2 | `!string.Equals(originalPost.Visibility, VisibilityValues.Public, OrdinalIgnoreCase)` (`:74-75`) | `InvalidOperationException("Only public posts can be shared.")` |
| 3 | `originalPost.User?.IsPrivate ?? true` (`:79-81`, null-safe: unknown author ⇒ private) | `InvalidOperationException("Posts from private accounts cannot be shared.")` |
| 4 | Bidirectional block between sharer and author (`:83-85`) | `InvalidOperationException("Sharing is not allowed between blocked users.")` |

Read-side privacy mirrors this: single-post reads deny private posts unless the viewer has an **accepted follow** (`Followers.Any(f => f.FollowerId == userId && f.FollowingId == post.UserId && f.Accepted)`, `:248-249`; anonymous viewers get a login message). Profile lists return **empty** for private targets without an accepted follow (`:300-301`; `KeyNotFoundException` if the user does not exist). The profile query predicate is `(isOwner || p.Visibility == VisibilityValues.Public)` (`:313`) — note exact `==` here, while share/single compare with `OrdinalIgnoreCase`. Feed shows public posts plus own posts (`p.UserId == viewerId` bypass, `:370`) plus accepted-follow private posts (`:371-374`).

Media on quoted posts is eager-loaded at **every** ancestor depth (`ParentPost.Media`, see §4), so share galleries render.

### 2.3 Two-way block enforcement

One predicate shape, used in both directions everywhere (`(me→them) || (them→me)`):

- Feed — `FeedQueryForViewer` (`:376-378`):
  ```csharp
  !_context.BlockUsers.Any(b => (b.UserId == viewerId && b.BlockedUserId == p.UserId)
                             || (b.UserId == p.UserId && b.BlockedUserId == viewerId))
  ```
- Single post — `GetPostByIdAsync` (`:231-233`): same shape against `post.UserId`; throws `UnauthorizedAccessException`.
- Profile list — `GetPostsByUserIdAsync` (`:280-282`): same shape; returns **empty** (not 401).
- Share — `:83-85` (see §2.2): `InvalidOperationException`.
- Beyond posts (ADR-009): Follow/Like/Comment writes carry `BlockUsers.AnyAsync` gates (`InvalidOperationException` → HTTP 400); follower/following/pending/search lists are block-filtered; blocked profile reads throw `UnauthorizedAccessException` (401); `NotificationRepository.AddAsync` centrally suppresses notifications to/from blocked parties.

`GetMyPostsAsync` is the only post read that skips the block filter (you always see your own posts).

### 2.4 Concurrency & atomic counter updates

All mutating post operations open an explicit transaction (`BeginTransactionAsync` at `:19` Add, `:57` Share, `:136` Delete) and update counters with **set-based** `ExecuteUpdateAsync` — never read-modify-write on tracked entities:

```csharp
// Add (:35-37) — runs BEFORE the insert as an existence pre-check too
.Where(u => u.Id == post.UserId)
.ExecuteUpdateAsync(s => s.SetProperty(u => u.PostsCount, u => u.PostsCount + 1));
// Share (:98-100, :105-107)
... PostsCount + 1 ...; .Where(p => p.Id == postId && !p.IsDeleted)
... ShareingsCount + 1 ...;
// Delete (:152-154)
... PostsCount > 0 ? PostsCount - 1 : 0 ...;
```

Every counter write is followed by a `rows == 0` guard (`:39-40`, `:102-103`, `:109-110` throw `KeyNotFoundException("...rolled back")`; `:149-150` returns `false`). The pre-check doubles as a user-existence check, the rows guard as the race guard (user deleted between check and write). Pagination is deterministic: `OrderByDescending(CreatedAt).ThenByDescending(Id)` (`:201-202`, `:314-315`; feed `:379`), so concurrent inserts cannot shift pages. `page < 1` or `limit < 1` **throw** `ArgumentOutOfRangeException`; `limit` is clamped above at `MaxPageLimit = 50` (`:11`, `:414-430`); `Skip((page-1)*limit).Take(limit)` with defaults `page = 1, limit = 20`.

## 3. Database engine & indexing (MySQL 8 / EF Core 9)

Design constraints: MySQL `utf8mb4` index limit is 3072 bytes, so indexed string FKs are `HasMaxLength(255)` (matching `AspNetUsers.Id`); 450 would overflow two-column composites (ADR-006). All `DateTime`/`DateTime?` properties get UTC converters + `SetPrecision(6)`.

### 3.1 Posts

```csharp
HasIndex(p => p.UserId)                                                     // :35 single-col (FK support, avoids MySQL 1553)
HasIndex(p => p.ParentPostId)                                               // :36
HasIndex(p => new { p.CreatedAt, p.Id }).IsDescending(true, true)           // :39-44 feed sort key
HasIndex(p => new { p.UserId, p.CreatedAt, p.Id })                          // :47-53 covering (profile pages)
    .IsDescending(false, true, true)
HasIndex(p => new { p.Visibility, p.CreatedAt, p.Id })                      // :56-62 covering (public discovery)
    .IsDescending(false, true, true)
Property(p => p.Visibility).HasMaxLength(20)                                // :64-65
Property(p => p.UserId/ParentPostId).HasMaxLength(255)                      // :69-73
```

Single-column FK indexes are kept **alongside** the composites: MySQL error 1553 ("cannot drop index needed in a foreign key constraint") otherwise blocks migrations (see `AddIndexes` idempotent `drop_fk_if_exists` procedure, ADR per 2026-09-15 session).

### 3.2 Social graph & engagement

- **Followers:** `HasIndex(f => f.FollowerId)`, `HasIndex(f => f.FollowingId)`, `HasIndex(f => new { f.FollowerId, f.FollowingId }).IsUnique()`, `HasIndex(f => new { f.FollowerId, f.Accepted, f.FollowingId })`, `HasIndex(f => new { f.FollowingId, f.Accepted })`; both IDs `HasMaxLength(255)`.
- **BlockUser:** PK stays `Id` (zero breaking changes — DTOs/tests depend on it); `HasIndex(b => b.UserId)`, `HasIndex(b => b.BlockedUserId)`, composite `HasIndex(b => new { b.BlockedUserId, b.UserId })` for the bidirectional checks; all three `HasMaxLength(255)`.
- **Like:** `HasIndex(l => l.PostId)`, `HasIndex(l => l.UserId)`, `HasIndex(l => new { l.UserId, l.PostId }).IsUnique()` (one like per user/post); IDs `HasMaxLength(255)`.
- **Comment:** `HasIndex(c => c.PostId)`, `HasIndex(c => c.UserId)`, `HasIndex(c => c.ParentId)`, `HasIndex(c => new { c.PostId, c.CreatedAt }).IsDescending(false, true)`; all three IDs `HasMaxLength(255)`.
- **Media:** `HasIndex(m => m.PostId)`, `HasIndex(m => m.UserId)`; both `HasMaxLength(255)`.

### 3.3 Notifications / telemetry / identity

- **Notification:** `HasIndex(n => n.UserId)`, `HasIndex(n => n.RecipientId)`, `HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt })`, `HasIndex(n => new { n.RecipientId, n.IsRead, n.Priority, n.CreatedAt })` (smart-inbox order), `HasIndex(n => new { n.RecipientId, n.GroupKey, n.IsRead })` (aggregation); `GroupKey.HasMaxLength(300)`, `LastActorName.HasMaxLength(120)`; new columns `GroupKey/ActorCount/LastActorName/Priority/IsDeferred` + `NotificationPreference` entity (per-type toggles, UTC-hour quiet window with wrap support, digest mode) — ADR-010.
- **RequestLog:** indexes on `CreatedAt`, `UserId`, `Endpoint`, `StatusCode`, plus `(CreatedAt, StatusCode)` and `(Endpoint, CreatedAt)` range scans; `Endpoint` (255, required), `HttpMethod` (10, required), `IpAddress` (45), `UserAgent` (512) — ADR-011.
- **DailyMetricSnapshot:** `HasIndex(s => s.Date).IsUnique()`.
- **RefreshToken:** `HasIndex(r => r.Token).IsUnique()`, `HasIndex(r => r.UserId)`; `Token.HasMaxLength(512)`, `UserId` 255.

## 4. Query engine & split queries

`LoadCompletePostsQuery` (`:393-406`) eagerly loads **3 ancestor levels** (4 posts total) with user + media at every level:

```csharp
Include(p => p.User)                                        // :397
Include(p => p.Media)                                       // :398
ParentPost -> { User, Media }                               // :399-400
ParentPost.ParentPost -> { User, Media }                    // :401-402
ParentPost.ParentPost.ParentPost -> { User, Media }         // :403-404
.AsSplitQuery()                                             // :405
```

Comment at `:386-388`: split queries prevent **Cartesian explosion** from the parallel `ParentPost` branches (a single query would multiply rows across user × media × 3 ancestor levels). `AsSplitQuery()` is applied **after** `Skip/Take` pagination, so page boundaries are computed on the root posts only, then each include is fetched with a separate round-trip. Likes for the viewer are batched in one query covering the 3-level parent chain (no N+1). `FeedQueryForViewer` centralizes feed predicates so feed/profile/single cannot drift.

Media writes reconcile **by `Id` only** (`ReconcileMediaAsync`, `:472-511`): both sides go through `Where(!IsNullOrWhiteSpace Id).DistinctBy(m => m.Id).ToDictionary` (`:474-479`, duplicate client keys cannot crash); missing ⇒ `Remove`, matched ⇒ update `Name/Type/Url/ThumbnailUrl ?? current` + `UpdatedAt = UtcNow` (`:491-495`), new/blank-Id ⇒ insert with fresh `Guid`, `PostId = existing.Id`, `UserId = existing.UserId` (`:499-509`).

## 5. Data flow (request lifecycle)

```
HTTP → RequestTelemetryMiddleware (Stopwatch, /api-only, never throws)
    → TokenBlacklistMiddleware (header + admin_token cookie, blacklisted_user:{sub})
    → GlobalExceptionMiddleware (ValidationException→400 + error dict; KeyNotFound→404;
                                 InvalidOperation→400; UnauthorizedAccess→401)
    → Controller → MediatR ValidationBehavior (FluentValidation) → Handler
    → Repository (transaction + ExecuteUpdateAsync + split-query reads)
    → MySQL / Redis-or-memory cache
```

Errors are JSON `{ success, message, data, errors }`. Pagination query params on post reads are **`Page`/`Limit`** (capitalized, matching C# DTO binding); most other controllers use lowercase `page`/`limit` — see `API_REFERENCE.md`.
