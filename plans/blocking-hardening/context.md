# Context: blocking-hardening

## Key files
- `Social.Infrastructure/Repositories/FollowRepository.cs` — no block checks (24-84, 134-182, 213-274)
- `Social.Infrastructure/Repositories/LikeRepository.cs` — no block checks (20-90)
- `Social.Infrastructure/Repositories/CommentRepository.cs` — no block checks (25-84, 201-230)
- `Social.Infrastructure/Repositories/UserRepository.cs` — `GetUserByIdAsync:68-95`, `SearchUsers:165-315` no block filter
- `Social.Application/Features/Notifications/Commands/CreateNotificationCommand.cs` — no gate
- `Social.Application/Features/Admin/Users/Commands/BanUserCommand.cs:34,45,52` — self-ban only, no Admin-target guard, TTL 365d vs DB 100y
- `Social.Application/Features/Admin/Users/Commands/UnbanUserCommand.cs:39` — hardcoded `active-user-001`, incomplete guard
- `Social.Infrastructure/Repositories/AdminRepository.cs:88-96` — unlock leaves `LockoutEnabled=true`
- `Social/Middlewares/TokenBlacklistMiddleware.cs:27-36` — token-only, never reads `blacklisted_user`
- `Social.Application/Features/Users/Commands/RefreshTokenCommand.cs:38-56` — no lockout check
- `Social/Program.cs:135-139` — middleware runs after auth (correct for user-claim check, keep)
- `GlobalExceptionMiddleware` — InvalidOperation->400, Unauthorized->401, KeyNotFound->404, Argument->400

## Decisions
- Write-path block violations throw `InvalidOperationException` (400); blocked profile reads throw `UnauthorizedAccessException` (401); mirrors `PostRepository` precedent.
- Middleware reads `ICacheService blacklisted_user:{id}` directly to avoid `ITokenService` contract break.
- Refresh gate uses DB `User.LockoutEnd > now` (no new deps, no ctor breaks).
- Likes/comments list reads left unfiltered (no viewer param in contracts); post-level gate already protects.
- Test double `TestAdminRepository` must mirror prod fixes (unlock clears flag, no hardcode).

## Open questions
- None. User chose full hardening + harden ban.
