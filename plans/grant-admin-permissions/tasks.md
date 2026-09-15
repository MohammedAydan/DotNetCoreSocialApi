# Tasks: Grant Admin Permissions

- [x] Task 1: Core & Infrastructure Database Seeder
  - [x] Define `IDatabaseSeeder` in `Social.Core/Interfaces/IDatabaseSeeder.cs`
  - [x] Implement `DatabaseSeeder` in `Social.Infrastructure/Services/DatabaseSeeder.cs`
  - [x] Register `IDatabaseSeeder` in `Social.Infrastructure/DependencyInjection.cs`
- [x] Task 2: Startup Seeding in Program.cs & Login Elevation
  - [x] Add non-testing environment startup seeder invocation in `Social/Program.cs`
  - [x] Update `Social/Controllers/Admin/AdminDashboardController.cs` to support automatic elevation and initial admin bootstrap for `mohammedaydan12@gmail.com`
- [x] Task 3: Verification & Tests
  - [x] Run full test suite (`dotnet test Social.sln -c Release` - 130/130 passing)
  - [x] Verify 0 compiler errors (`dotnet build Social.sln -c Release`)
- [x] Task 4: Documentation & Closure
  - [x] Write `plans/grant-admin-permissions/review.md`
  - [x] Update `plans/context.md` and `plans/SESSION_LOG.md`
