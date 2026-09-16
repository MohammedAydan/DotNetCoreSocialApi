# API Reference — Social API v1

> Contract source: `Social/Social.API.json` (OpenAPI **3.0.1**, `info: Social API / v1`, **no `servers` entry**, global `security: [{ Bearer: [] }]`).
> Auth scheme (`components.securitySchemes.Bearer`): `type: http, scheme: bearer, bearerFormat: JWT` — send `Authorization: Bearer <token>`.
> **83 operations across 76 paths, 11 tags.** Every `operationId` is **empty** in the spec (Orval auto-names apply — see `SDK_WEB.md`). Every operation declares only a `200` response with description `OK` and **no content schema**; actual bodies use the envelope below.
> Pagination: post reads use **`Page`/`Limit`** (capitalized); all other paginated reads use lowercase **`page`/`limit`** (admin lists use `page`/`pageSize`). Defaults `Page|page = 1`, `Limit|limit = 20` (comments-by-post `limit = 10`, admin users `pageSize = 10`). No `minimum`/`maximum` in the spec — enforcement is server-side (`page|limit < 1` throws 400; `limit` clamped to 50; `ARCHITECTURE.md` §2.4).

## 1. Conventions (apply to every endpoint)

### 1.1 Headers

| Header | Required | Value |
|--------|----------|-------|
| `Authorization` | Yes, except register/sign-in/refresh/forget/reset (which still list bearer in-spec — send **no** token there) | `Bearer <JWT access token>` |
| `Content-Type` | Yes on POST/PUT with body | `application/json` |

Unauthenticated requests get the framework 401 challenge. The browser admin console alternatively carries the `admin_token` cookie (ADR-005).

### 1.2 Envelope (verified: `Social.Core/Common/ApiResponse.cs`, camelCase on the wire)

```jsonc
// success (HTTP 200)
{ "success": true, "message": "Feed retrieved successfully", "data": [ /* … */ ], "errors": null }
// error (HTTP 4xx/5xx — Social/Middlewares/GlobalExceptionMiddleware.cs)
{ "success": false, "message": "<reason>", "data": null, "errors": [{ "field": "Page", "error": "…" }] }
```

`errors` is populated only for FluentValidation failures (400); otherwise `null`.

### 1.3 Error matrix (verified middleware mapping + framework behavior)

| Status | Produced by | Business triggers (non-exhaustive) |
|--------|-------------|------------------------------------|
| 400 | `FluentValidation.ValidationException`, `ArgumentException`, `InvalidOperationException` | bad `Page`/`Limit` (`< 1` throws); share-policy violations (non-public, private author, deleted target, blocked parties); block-gated Follow/Like/Comment writes; ban with empty reason / non-positive duration / Admin target; `q` / date-range validation |
| 401 | missing/invalid/expired JWT; `UnauthorizedAccessException` | blocked profile/post reads; revoked (`blacklisted_user:{sub}`) or banned callers; refresh with `LockoutEnd > now` |
| 403 | `[Authorize(Roles = "Admin[,Moderator]")]` | non-admin hitting `/api/admin/**`; non-admin cookie on `/admin` |
| 404 | `KeyNotFoundException` | missing/deleted post, comment, like target, user, notification; share target deleted; counter race-guard (`rows == 0`) |
| 409 | — | **Not currently emitted**: no `Conflict`/409 path exists in `Social/` or the middleware. Reserved for future duplicate-conflict semantics |
| 500 | unhandled exception | never-throw telemetry excluded; audit/notification fallbacks degrade gracefully |

### 1.4 Body-schema map (spec `requestBody` → TS model in `sdks/web/models/`)

`BlockUserRequest`, `CreateCommentRequest`, `CreateReplyCommentRequest`, `UpdateCommentRequest`, `FollowRequest`, `LikeRequest` (`{ postId?: string }`), `CreateNotificationDto`, `UpdateNotificationDto`, `NotificationPreferenceDto`, `CreatePostRequest`, `UpdatePostRequest`, `SharePostRequest`, `CreateUserRequest`, `SignIn` (`{ email: string; password: string }`), `RefreshTokenRequest`, `ForgetPasswordRequest`, `ResetPasswordRequest`, `ChangePasswordRequest`, `UpdateUserDto`, `AdminModerationActionRequest`, `AdminUpdateVisibilityRequest`, `AdminBanUserRequest`, `AdminUnbanUserRequest`, `AdminUpdateRolesRequest`, `AdminToggleVerificationRequest`, `AdminResetPasswordRequest`. Full field lists live next to the models (e.g. `sdks/web/models/signIn.ts`).

---

## 2. Posts & Timeline (`Posts`, 11 ops)

| Method & Route | Auth | Params | Body | Success 200 | Errors |
|----------------|------|--------|------|-------------|--------|
| `POST /api/Posts` — create post, atomic `PostsCount + 1` (pre-check + rows guard) | Bearer | — | `CreatePostRequest` | envelope, `data` = created post | 400 validation / missing user; 401 |
| `PUT /api/Posts` — update own post, media reconciled **by `Id`** (delete-missing / update / insert; `DistinctBy(Id)`) | Bearer | — | `UpdatePostRequest` | envelope, `data` = updated post | 400 validation; 401; 404 missing/deleted/foreign |
| `POST /api/Posts/share` — share with strict policy + `PostsCount`/`ShareingsCount` increments | Bearer | — | `SharePostRequest` | envelope, `data` = share post | 400 non-public / private-author / blocked-parties; 404 deleted target; 401 |
| `GET /api/Posts/my-posts` — authenticated user's posts (no block filter; `!IsDeleted`) | Bearer | `Page` (d1), `Limit` (d20) | — | envelope, `data` = page (`CreatedAt DESC, Id DESC`) | 400 `Page\|Limit < 1`; 401 |
| `GET /api/Posts/user/{userId}` — profile posts. Owner sees own incl. non-public; others see `Public` only; private target + no accepted follow ⇒ **empty page** | Bearer | `userId` path req; `Page`, `Limit` | — | envelope, `data` = page | 400 bad paging; 401 blocked-either-direction (empty); 404 unknown user |
| `GET /api/Posts/feed` — followed + own + public discovery; two-way block filtered; accepted-follow private included; deleted masked | Bearer | `Page` (d1), `Limit` (d20) | — | envelope, `data` = page, deterministic order | 400 bad paging; 401 |
| `GET /api/Posts/{postId}` — detail with like status + 3-level parent chain; deleted parent content masked for non-owners | Bearer | `postId` path req | — | envelope, `data` = post + `ParentPost…` + media at every depth | 401 blocked / private-no-follow; 404 missing/deleted |
| `DELETE /api/Posts/{postId}` — **soft** delete (`IsDeleted = true`, `PostsCount − 1` floored); shares kept | Bearer | `postId` path req | — | envelope, `data` = `false` if already gone, else success | 401; 404 foreign/missing |
| `POST /api/posts/{postId}/report` — report a post for policy violation | Bearer | `postId` path req | `{ reason, details? }` | envelope | 400 duplicate open report / self-report / validation; 401; 404 missing/deleted |
| `GET /api/posts/reports/mine` — caller's reports, `CreatedAt DESC` | Bearer | `Page` (d1), `Limit` (d20) | — | envelope, `data` = page | 400 bad paging; 401 |
| `DELETE /api/posts/reports/{reportId}` — cancel own pending report | Bearer | `reportId` path req | — | envelope | 401; 404 missing/foreign |

---

## 3. Comments (`Comments`, 7 ops)

| Method & Route | Auth | Params | Body | Success 200 | Errors |
|----------------|------|--------|------|-------------|--------|
| `POST /api/Comments` — comment on a post | Bearer | — | `CreateCommentRequest` | envelope, `data` = comment | 400 validation / blocked-parties gate; 401; 404 missing post |
| `POST /api/Comments/reply` — reply to a comment | Bearer | — | `CreateReplyCommentRequest` | envelope, `data` = reply | 400 validation / block gate; 401; 404 missing parent |
| `PUT /api/Comments/{commentId}` — edit own comment | Bearer | `commentId` path req | `UpdateCommentRequest` | envelope, `data` = comment | 400; 401; 404 missing/foreign |
| `DELETE /api/Comments/{commentId}` — delete own comment | Bearer | `commentId` path req | — | envelope | 401; 404 missing/foreign |
| `GET /api/Comments/{commentId}` — single comment | Bearer | `commentId` path req | — | envelope, `data` = comment | 401; 404 |
| `GET /api/Comments/post/{postId}` — comments on a post | Bearer | `postId` path req; `page` (d1), `limit` (d10) | — | envelope, `data` = page | 400 bad paging; 401; 404 missing post |
| `GET /api/Comments/replies/{parentId}` — replies to a comment | Bearer | `parentId` path req | — | envelope, `data` = list | 401; 404 missing parent |

## 4. Likes (`Like`, 2 ops)

| Method & Route | Auth | Params | Body | Success 200 | Errors |
|----------------|------|--------|------|-------------|--------|
| `POST /api/Like` — like a post (unique per user+post; block-gated) | Bearer | — | `LikeRequest` (`{ postId?: string }`) | envelope, `data` = like | 400 validation / blocked / duplicate; 401; 404 missing post |
| `GET /api/Like/{postId}` — likes on a post | Bearer | `postId` path req; `page` (d1), `limit` (d20) | — | envelope, `data` = page | 400 bad paging; 401; 404 missing post |

## 5. Follow graph (`Follow`, 7 ops)

| Method & Route | Auth | Params | Body | Success 200 | Errors |
|----------------|------|--------|------|-------------|--------|
| `POST /api/Follow/follow` — follow (block-gated both directions; private target ⇒ pending request) | Bearer | — | `FollowRequest` | envelope, `data` = follow/request | 400 blocked / self / validation; 401; 404 unknown target |
| `POST /api/Follow/unfollow` — unfollow | Bearer | — | `FollowRequest` | envelope | 400 validation; 401; 404 no relation |
| `POST /api/Follow/accept-follow-request` — accept pending (follower+following pair) | Bearer | — | `FollowRequest` | envelope | 400 validation; 401; 404 no pending |
| `POST /api/Follow/reject-follow-request` — reject pending | Bearer | — | `FollowRequest` | envelope | 400 validation; 401; 404 no pending |
| `GET /api/Follow/followers` — my followers (block-filtered) | Bearer | `page` (d1), `limit` (d20) | — | envelope, `data` = page | 400 bad paging; 401 |
| `GET /api/Follow/following` — my following (block-filtered) | Bearer | `page` (d1), `limit` (d20) | — | envelope, `data` = page | 400 bad paging; 401 |
| `GET /api/Follow/pending-follow-requests` — inbound pending (block-filtered) | Bearer | `page` (d1), `limit` (d20) | — | envelope, `data` = page | 400 bad paging; 401 |

## 6. Blocking (`BlockUser`, 4 ops)

| Method & Route | Auth | Params | Body | Success 200 | Errors |
|----------------|------|--------|------|-------------|--------|
| `POST /api/BlockUser/block` — block a user (writes gate all future interactions both ways) | Bearer | — | `BlockUserRequest` | envelope | 400 validation / self-block; 401; 404 unknown target |
| `POST /api/BlockUser/unblock` — unblock | Bearer | — | `BlockUserRequest` | envelope | 400 validation; 401; 404 no block |
| `GET /api/BlockUser/blocked-users` — my block list | Bearer | `page` (d1), `limit` (d20) | — | envelope, `data` = page | 400 bad paging; 401 |
| `GET /api/BlockUser/is-blocked` — check one direction | Bearer | `blockedUserId` query opt | — | envelope, `data` = bool | 401 |

## 7. Notifications (`Notifications`, 13 ops)

Write-time pipeline (ADR-010): block gate → self-skip → preference toggle → quiet-hours defer → aggregate-or-insert (`X and N others…`). Moderation notices bypass toggles/defer/aggregation. Inbox order: `IsRead, Priority DESC, CreatedAt DESC`; quiet rows auto-release on inbox/badge reads outside the window.

| Method & Route | Auth | Params | Body | Success 200 | Errors |
|----------------|------|--------|------|-------------|--------|
| `POST /api/Notifications` — create (legacy direct create) | Bearer | — | `CreateNotificationDto` | envelope, `data` = notification | 400 validation; 401 |
| `GET /api/Notifications/{id}` — single (owner only) | Bearer | `id` path req | — | envelope | 401 foreign; 404 |
| `PUT /api/Notifications/{id}` — update (owner only) | Bearer | `id` path req | `UpdateNotificationDto` | envelope | 400; 401 foreign; 404 |
| `DELETE /api/Notifications/{id}` — delete (owner only) | Bearer | `id` path req | — | envelope | 401 foreign; 404 |
| `GET /api/Notifications/user/{userId}` — legacy per-user page | Bearer | `userId` path req; `page`, `limit` (d1/d20) | — | envelope, `data` = page | 400; 401 foreign |
| `GET /api/Notifications/user/{userId}/unread` — legacy unread page | Bearer | `userId` path req; `page` (d1), `limit` (d20) | — | envelope | 400; 401 foreign |
| `POST /api/Notifications/{id}/mark-read` — mark one read (owner) | Bearer | `id` path req | — | envelope | 401 foreign; 404 |
| `POST /api/Notifications/user/{userId}/mark-all-read` — mark all read | Bearer | `userId` path req | — | envelope | 401 foreign |
| `DELETE /api/Notifications/user/{userId}/all` — delete all | Bearer | `userId` path req | — | envelope | 401 foreign |
| `GET /api/Notifications/inbox` — smart inbox | Bearer | `type` opt; `unreadOnly` bool (dFalse); `page` (d1), `limit` (d20) | — | envelope, `data` = `{ items, total, unreadCount }` | 400 bad paging; 401 |
| `GET /api/Notifications/unread-count` — badge count | Bearer | — | — | envelope, `data` = int | 401 |
| `GET /api/Notifications/preferences` — my toggles/quiet/digest | Bearer | — | — | envelope, `data` = preference | 401 |
| `PUT /api/Notifications/preferences` — upsert preferences | Bearer | — | `NotificationPreferenceDto` | envelope, `data` = preference | 400 validation; 401 |

## 8. Identity (`User`, 14 ops — includes 2 dashboard aliases)

| Method & Route | Auth | Params | Body | Success 200 | Errors |
|----------------|------|--------|------|-------------|--------|
| `POST /api/User/register` — register (no token needed despite in-spec bearer flag) | — | — | `CreateUserRequest` | envelope, `data` = user/auth | 400 validation / duplicate email |
| `POST /api/User/sign-in` — sign in, returns JWT + refresh | — | — | `SignIn` (`{ email, password }`) | envelope, `data` = tokens + user | 400 validation; 401 bad credentials / locked-out (banned) |
| `POST /api/User/refresh-token` — rotate refresh token; rejects `LockoutEnd > now` | — | — | `RefreshTokenRequest` | envelope, `data` = new tokens | 400 validation; 401 revoked/expired/banned |
| `POST /api/User/forget-password` — start reset flow | — | — | `ForgetPasswordRequest` | envelope | 400 validation |
| `POST /api/User/reset-password` — complete reset | — | — | `ResetPasswordRequest` | envelope | 400 validation/expired token |
| `POST /api/User/change-password` — change (authenticated) | Bearer | — | `ChangePasswordRequest` | envelope | 400 validation/wrong current; 401 |
| `POST /api/User/logout` — logout (revokes refresh) | Bearer | — | — | envelope | 401 |
| `GET /api/User/get-user` — current profile | Bearer | — | — | envelope, `data` = user | 401 |
| `GET /api/User/get-user/{userId}` — profile by id (blocked ⇒ 401) | Bearer | `userId` path req | — | envelope | 401 blocked; 404 |
| `GET /api/User/search` — user search (block-filtered) | Bearer | `q` opt, `userId` opt, `page` (d1), `limit` (d20) | — | envelope, `data` = page | 400 bad paging; 401 |
| `PUT /api/User/update-user` — edit profile (`IsVerified` is admin-only, ignored here) | Bearer | — | `UpdateUserDto` | envelope, `data` = user | 400 validation; 401 |
| `DELETE /api/User/delete-user` — delete own account | Bearer | — | — | envelope | 401 |
| `POST /api/dashboard/User/sign-in` — dashboard alias of sign-in | — | — | `SignIn` | envelope (same) | same as sign-in |
| `POST /api/dashboard/User/refresh-token` — dashboard alias of refresh | — | — | `RefreshTokenRequest` | envelope (same) | same as refresh |

## 9. Admin analytics (`AdminAnalytics`, 8 ops — `Admin`/`Moderator`)

| Method & Route | Auth | Params | Body | Success 200 | Errors |
|----------------|------|--------|------|-------------|--------|
| `GET /api/admin/analytics/overview` — legacy totals | Admin/Mod | — | — | envelope, `data` = totals | 401/403 |
| `GET /api/admin/analytics/diagnostics` — legacy system health | Admin/Mod | — | — | envelope | 401/403 |
| `GET /api/admin/analytics/kpi-summary` — snapshot-first KPIs | Admin/Mod | — | — | envelope, `data` = KPI ribbon | 401/403 |
| `GET /api/admin/analytics/user-growth` — per-day users curve | Admin/Mod | `range` opt (default `30d`) | — | envelope, `data` = series | 400 bad range; 401/403 |
| `GET /api/admin/analytics/content-velocity` — posts/shares/likes/comments per day | Admin/Mod | `days` opt int (d30) | — | envelope | 400 bad days; 401/403 |
| `GET /api/admin/analytics/api-health` — latency P50–P99, endpoint matrix, 4xx/5xx | Admin/Mod | — | — | envelope | 401/403 |
| `GET /api/admin/analytics/safety-metrics` — moderation/ban density | Admin/Mod | `days` opt int (d30) | — | envelope | 400; 401/403 |
| `GET /api/admin/analytics/request-stream` — live request rows (poll 15–30 s) | Admin/Mod | `take` opt int (d50) | — | envelope, `data` = rows | 400 bad take; 401/403 |

## 10. Admin audit (`AdminAuditLogs`, 1 op — `Admin`)

| Method & Route | Auth | Params | Body | Success 200 | Errors |
|----------------|------|--------|------|-------------|--------|
| `GET /api/admin/audit-logs` — append-only admin-action trail | Admin | `page` (d1), `pageSize` (d20), `actionType` opt, `adminId` opt, `fromDate`/`toDate` opt datetimes | — | envelope, `data` = page | 400 bad range/paging; 401/403 |

## 11. Admin moderation (`AdminModeration`, 10 ops — `Admin`/`Moderator`)

| Method & Route | Auth | Params | Body | Success 200 | Errors |
|----------------|------|--------|------|-------------|--------|
| `GET /api/admin/moderation/feed` — combined post+comment moderation slice | Admin/Mod | `page` (d1), `pageSize` (d20) | — | envelope | 400; 401/403 |
| `POST /api/admin/moderation/posts/{postId}/hide` — hide + `ModerationNotice` to author + audit | Admin/Mod | `postId` path req | `AdminModerationActionRequest` | envelope | 400 validation; 401/403; 404 |
| `POST /api/admin/moderation/posts/{postId}/restore` — restore + notice + audit | Admin/Mod | `postId` path req | `AdminModerationActionRequest` | envelope | same |
| `POST /api/admin/moderation/comments/{commentId}/hide` — hide comment + notice | Admin/Mod | `commentId` path req | `AdminModerationActionRequest` | envelope | same |
| `POST /api/admin/moderation/comments/{commentId}/restore` — restore comment + notice | Admin/Mod | `commentId` path req | `AdminModerationActionRequest` | envelope | same |
| `POST /api/admin/moderation/posts/{postId}/visibility` — set `public\|followers_only\|private` + notice | Admin/Mod | `postId` path req | `AdminUpdateVisibilityRequest` | envelope | 400 bad visibility; 401/403; 404 |
| `DELETE /api/admin/moderation/posts/{postId}` — **permanent** delete + media cleanup + notice | Admin/Mod | `postId` path req; `reason` query opt | — | envelope | 401/403; 404 |
| `GET /api/admin/moderation/reports` — paged report queue with excerpt + open counts | Admin/Mod | `status` opt; `page` (d1), `pageSize` (d20) | — | envelope, `data` = `{ items, totalCount, page, pageSize }` | 400 bad status; 401/403 |
| `GET /api/admin/moderation/reports/{id}` — single report | Admin/Mod | `id` path req | — | envelope | 401/403; 404 |
| `POST /api/admin/moderation/reports/{id}/resolve` — `dismiss\|hide_post` + audit + notify | Admin/Mod | `id` path req | `{ action, note? }` | envelope | 400 invalid transition; 401/403; 404 |

## 12. Admin users (`AdminUsers`, 6 ops — `Admin`)

Ban semantics (ADR-009): requires non-empty reason + positive duration, refuses Admin targets, aligns `blacklisted_user:{sub}` cache TTL with the DB lock (36500 d = indefinite), records duration in audit. Unban guard is `LockoutEnd > now`; unlock clears `LockoutEnabled`.

| Method & Route | Auth | Params | Body | Success 200 | Errors |
|----------------|------|--------|------|-------------|--------|
| `GET /api/admin/users` — identity directory | Admin | `page` (d1), `pageSize` (d10), `q` opt | — | envelope, `data` = page (incl. `CreatedAt`) | 400; 401/403 |
| `POST /api/admin/users/{userId}/ban` — lockout + refresh revoke + instant JWT kill + audit | Admin | `userId` path req | `AdminBanUserRequest` | envelope | 400 empty reason / bad duration / Admin target; 401/403; 404 |
| `POST /api/admin/users/{userId}/unban` — unlock + audit | Admin | `userId` path req | `AdminUnbanUserRequest` | envelope | 400 not banned; 401/403; 404 |
| `POST /api/admin/users/{userId}/roles` — set roles (self-demotion prevented) | Admin | `userId` path req | `AdminUpdateRolesRequest` | envelope | 400 validation/self-demotion; 401/403; 404 |
| `POST /api/admin/users/{userId}/verify` — toggle `IsVerified` (only path — profile edits ignore it) | Admin | `userId` path req | `AdminToggleVerificationRequest` | envelope | 400; 401/403; 404 |
| `POST /api/admin/users/{userId}/reset-password` — admin password reset | Admin | `userId` path req | `AdminResetPasswordRequest` | envelope | 400 validation; 401/403; 404 |

*Count check: §2–§12 = 11+7+2+7+4+13+14+8+1+10+6 = 83 operations / 76 paths. No stubs — every operation in `Social.API.json` appears above.*
