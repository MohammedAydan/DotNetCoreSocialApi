# Plan: blocking-hardening

## Goal
Enforce user-to-user blocks across all interactions and harden admin platform-wide ban so banned users lose access immediately and cannot bypass via refresh tokens.

## Acceptance criteria
- [ ] Blocked users cannot follow / accept-follow / like / comment on each other's content (write path returns 400).
- [ ] Blocked users excluded from profile reads, search, follower/following lists.
- [ ] Blocked actors do not trigger notifications to the other party.
- [ ] Admin ban requires non-empty reason, rejects zero/negative durations, refuses to ban other Admins.
- [ ] Banned user: live JWT rejected via middleware (401), refresh rejected, sign-in rejected.
- [ ] Unban removes hardcoded test ID, correctly detects locked state, fully unlocks, idempotent second call fails cleanly.
- [ ] `dotnet build Social.sln -c Release` 0 errors; full test suite passes.

## Approach
1. User blocks (Infrastructure writes): add bidirectional `BlockUsers.AnyAsync` guards in `FollowRepository`, `LikeRepository`, `CommentRepository`; filter `GetFollowers/GetFollowing/SearchUsers`; gate `GetUserByIdAsync` with viewer param; suppress notifications when blocked.
2. Admin ban: validate in `BanUserCommandHandler` (reason, duration, target-not-admin via `GetUserRolesAsync`); fix `UnlockUserAsync` to clear `LockoutEnabled`; fix `UnbanUserCommand` guard to `LockoutEnd > now` and drop `active-user-001` hardcode; align `blacklisted_user` TTL with DB lock; include duration in audit reason.
3. Token kill: extend `TokenBlacklistMiddleware` to decode sub/NameIdentifier and check `blacklisted_user:{id}` in `ICacheService` (keeps `ITokenService` contract unchanged); add DB lockout check in `RefreshTokenCommandHandler` via loaded `User.LockoutEnd`.
4. Tests: extend/verify existing Tier1-3 admin tests + block tests; add focused regression tests for block-gated follow/like and ban-refresh rejection.

## Scope IN
- `FollowRepository`, `LikeRepository`, `CommentRepository`, `UserRepository`, `Notification` gating, `Ban/UnbanCommand`, `AdminRepository.Unlock`, `TokenBlacklistMiddleware`, `RefreshTokenCommand`, `BanUserCommandValidator`.

## Scope OUT
- Read-path viewer filtering for likes/comments lists (no viewer param in contracts — would break interfaces); post feed already enforced; Moderator role expansion; audit persistence retry.

## Complexity
L
