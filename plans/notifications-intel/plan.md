# Plan: notifications-intel

## Goal
Turn flat notification CRUD into an intelligent in-app management system: write-time aggregation/dedup, priority-ordered smart inbox with unread badge, and per-user preferences with quiet hours — all in-app only (no email/SignalR).

## Acceptance criteria
- [ ] Repeat events (likes on same post, comments on same post, follows) collapse into one unread row with `ActorCount`, last actor, refreshed timestamp — no duplicate rows.
- [ ] Inbox returns `IsRead ASC, Priority DESC, CreatedAt DESC`, supports `type` + `unreadOnly` filters, paged with total + unread count; deferred (quiet-hours) items hidden until released.
- [ ] `GET /api/notifications/unread-count` badge excludes deferred items.
- [ ] Preferences per user: per-type toggles (like/comment/reply/follow/follow-request/share/mention), quiet-hours window (UTC hours), digest mode; moderation notices always delivered.
- [ ] Quiet-hours items persist as deferred and auto-release on next inbox/badge read outside the window.
- [ ] Fixed: `CreateNotification` requires `RecipientId` (currently never set → insert fails); `Delete`/`MarkAsRead` authorize by notification ownership, not by comparing notification-id to user-id.
- [ ] `dotnet build Social.sln -c Release` 0 errors; full suite green including new regression tests.

## Approach
1. Core: extend `Notification` (`GroupKey`, `ActorCount`, `LastActorName`, `Priority`, `IsDeferred`); new `NotificationPreference` entity; `NotificationPriority` + `NotificationGrouping` statics.
2. Infrastructure: DbContext config + indexes; `NotificationRepository.AddAsync` pipeline (block → self-skip → preference → quiet-defer → aggregate-or-insert); inbox/unread-count/preference/release methods; EF migration (schema only, never applied to prod without human approval).
3. Application: `GetInboxQuery`, `GetUnreadCountQuery`, `GetPreferenceQuery`, `UpdatePreferenceCommand` + validator; extend `NotificationDto`.
4. API: `inbox`, `unread-count`, `preferences` endpoints; ownership-auth fixes; `RecipientId` on create DTO/handler.
5. Tests: SQLite intelligence tests (aggregation, toggle-skip, quiet defer/release, ordering, moderation bypass).

## Scope IN
- Files under `Social.Core` (entity, preference, statics, repo contract), `Social.Infrastructure` (repo, DbContext, migration), `Social.Application/Features/Notifications` (CQRS, DTOs, validator), `Social/Controllers/NotificationsController.cs`, new unit tests.

## Scope OUT
- Email/push/SignalR delivery; read-path filtering of likes/comments lists; retention/TTL cleanup job; admin broadcast endpoint; per-device settings.

## Complexity
L
