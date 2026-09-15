# Context: Grant Admin Permissions

## Target User
- Email: `mohammedaydan12@gmail.com`
- Username: `mohammedaydan12`
- Initial Default Password: `AdminPassword123!`
- Roles: `Admin`, `Moderator`, `User`

## Files to Create / Modify
- `Social.Core/Interfaces/IDatabaseSeeder.cs` (create)
- `Social.Infrastructure/Services/DatabaseSeeder.cs` (create)
- `Social.Infrastructure/DependencyInjection.cs` (modify: register `IDatabaseSeeder`)
- `Social/Program.cs` (modify: call `seeder.SeedAsync()` on startup if not Testing)
- `Social/Controllers/Admin/AdminDashboardController.cs` (modify: support admin elevation and bootstrap)
- `plans/grant-admin-permissions/*` (plan artifacts)

## Dependencies Added
- None (uses existing ASP.NET Core Identity abstractions)

## Open Questions
- None.
