# Context: notifications-intel

## Key files
- `Social.Core/Entities/Notification.cs` — flat; `RecipientId` required, no grouping/priority/defer fields
- `Social.Core/NotificationActionTypes.cs` — like, comment, comment-reply, share, follow, follow-request, accept..., block...; moderation uses literal `"ModerationNotice"` in Admin handlers
- `Social.Core/Interfaces/INotificationRepository.cs` — CRUD + paged/unread, no count/inbox/preference APIs
- `Social.Infrastructure/Repositories/NotificationRepository.cs:21-36` — block gate only; no self-skip, prefs, aggregation
- `Social.Infrastructure/Data/ApplicationDbContext.cs:269-290` — Notification config + `(UserId,IsRead,CreatedAt)` index
- `Social.Application/Features/Notifications/Commands/CreateNotificationCommand.cs:21-33` — never sets `RecipientId` (broken insert); no `RecipientId` in `CreateNotificationDto`
- `Social/Controllers/NotificationsController.cs:139,160` — `IsAuthorizedUser(id)` compares notification-id to user-id (always 401); same in MarkAsRead
- Call sites skip self-notify except moderation; Like (LikeRepository.cs:81), Follow (FollowRepository.cs:297), Comment (CommentRepository.cs:236)

## Decisions
- Write-time aggregation (single row bump) over read-time grouping: keeps inbox query cheap and badge counts honest.
- Aggregate key: like→`like:post:{id}`, comment/reply→`comment:post:{id}`, follow/request/accept→`follow:{followerId}`; moderation never aggregates.
- ModerationNotice bypasses type toggles (safety-critical) but still deferred in quiet hours? No — delivered immediately, never deferred. Document in plan.
- Priority persisted at write (indexed ordering); static map in Core (no config table).
- Quiet hours as nullable UTC hour ints (0-23); wrap-around windows supported (22→7). No TimeOnly converter risk on Pomelo/SQLite.
- Digest mode: low-priority types (like/follow/share) aggregate into daily key `digest:{type}:{yyyyMMdd}` per recipient.
- Middleware/contracts unchanged; only additive `INotificationRepository` members (test doubles: check `TestAdminDoubles` — admin tests use own doubles, notification repo fakes live in unit tests via SQLite/NSubstitute).

## Open questions
- None. Scope chosen by user: aggregation + smart inbox + preferences, in-app only.
