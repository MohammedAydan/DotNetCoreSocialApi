# Feature Plan: Grant Full Admin Permissions to mohammedaydan12@gmail.com

## Goal
Grant full administrator permissions to `mohammedaydan12@gmail.com`, ensuring roles (`Admin`, `Moderator`, `User`), verified status, email confirmation, and reliable authentication via startup database seeding and intelligent login flow.

## Acceptance Criteria
1. `IDatabaseSeeder` and `DatabaseSeeder` service created in Clean Architecture (`Core` interface, `Infrastructure` implementation).
2. On startup (non-Testing environment), `DatabaseSeeder` ensures:
   - System roles (`Admin`, `Moderator`, `User`) exist.
   - User `mohammedaydan12@gmail.com` exists with verified status, confirmed email, unlocked status, and `Admin`, `Moderator`, `User` roles.
   - If user does not exist, creates the account with username `mohammedaydan12`, email `mohammedaydan12@gmail.com`, and initial password `AdminPassword123!`.
3. In `AdminDashboardController.cs` (`POST /admin/login`), if `mohammedaydan12@gmail.com` signs in:
   - If the account exists but lacks the `Admin` role, automatically elevate the account to `Admin` and issue the admin session.
   - If the account did not exist yet, initialize it and authenticate.
4. All existing 128 automated tests continue to pass with 0 regressions.
5. Zero compiler errors in Debug and Release configurations.

## Approach
1. Define `IDatabaseSeeder` in `Social.Core/Interfaces/IDatabaseSeeder.cs`.
2. Implement `DatabaseSeeder` in `Social.Infrastructure/Services/DatabaseSeeder.cs`.
3. Register `IDatabaseSeeder` in `Social.Infrastructure/DependencyInjection.cs`.
4. Run startup seeding in `Social/Program.cs` when not in `Testing` environment.
5. Enhance `AdminDashboardController.LoginSubmit` to ensure seamless elevation and initialization for `mohammedaydan12@gmail.com`.
6. Add integration test verifying seeder and role elevation.
7. Verify all tests pass and document in SESSION_LOG.md.
