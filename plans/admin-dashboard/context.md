# Admin Dashboard Context

## Files Touched / Created
- `Social.Core/Entities/AuditLog.cs`
- `Social.Core/Interfaces/IAuditLogRepository.cs`
- `Social.Core/Interfaces/IAdminRepository.cs`
- `Social.Infrastructure/Data/ApplicationDbContext.cs`
- `Social.Infrastructure/Repositories/AuditLogRepository.cs`
- `Social.Infrastructure/Repositories/AdminRepository.cs`
- `Social.Infrastructure/Repositories/UserRepository.cs`
- `Social.Application/Features/Admin/*`
- `Social/Controllers/Admin/*`
- `Social/Program.cs`
- `Social.Tests/Infrastructure/CustomWebApplicationFactory.cs`
- `Social.Admin.Web/Social.Admin.Web.csproj`
- `Social.Admin.Web/DependencyInjection.cs`
- `Social.Admin.Web/Models/*`
- `Social.Admin.Web/Services/*`
- `Social.Admin.Web/Components/*`
- `Social.Admin.Web/wwwroot/*`

## Dependencies Added
- None needed (ASP.NET Core 9 built-in features, MediatR, and EF Core already available)

## Environment Variables
- No new required env vars; diagnostics reads existing environment configs.

## Invariants & Rules
- Self-Ban protection: Admin cannot ban themselves (HTTP 400).
- Self-Demotion protection: Admin cannot remove Admin role from themselves (HTTP 400).
- Safe counter decrements: Counters like PostsCount or CommentsCount must never be decremented below 0.
- RBAC: Anonymous -> 401/redirect. User -> 403. Moderator -> 403 on User Management, 200 on Moderation actions. Admin -> 200.
- Audit Trail: Every administrative mutation generates an immutable `AuditLog` entry.
