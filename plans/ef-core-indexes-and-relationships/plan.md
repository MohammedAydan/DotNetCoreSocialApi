# Feature Plan: EF Core Indexes & Relationship Normalization

## Goal
Safely eliminate all unintended EF Core shadow foreign keys (`Post.UserId1`, `RefreshToken.UserId1`, `Comment.PostId1`, `Like.PostId1`, `Media.PostId1`), reconcile the EF Core model with the production MySQL database schema, and generate/apply a safe, clean migration (`AddIndexes`) adding the required performance indexes without data loss or destructive schema drops.

## Acceptance Criteria
1. ApplicationDbContext and entities have clean 1:N and 1:1 relationship configurations with zero shadow foreign keys.
2. The new migration contains only intended schema additions (indexes) and necessary reconciliations.
3. No destructive operations that break MySQL FK constraints or cause data loss.
4. Production database retains all existing data across all tables.
5. `dotnet ef database update` succeeds against production MySQL.
6. All solution tests pass (`dotnet test`).
7. Application runs and health checks pass.

## Approach
- Follow the 12-step mandatory workflow strictly.
- Inspect the codebase (entities, migrations, snapshot, ApplicationDbContext).
- Inspect the production MySQL database (read-only queries for table definitions, FK constraints, and indexes).
- Compare the three states (C# Model, ModelSnapshot, Live MySQL Schema).
- Correct the EF Core model configurations.
- Remove invalid unapplied `AddIndexes` migration.
- Generate new migration and inspect generated C# and SQL.
- Verify safe idempotent script.
- Apply migration and verify against live MySQL.

## Scope
- **IN**:
  - `Social.Infrastructure/Data/ApplicationDbContext.cs`
  - Entity navigation properties in `Social.Core/Entities/` (if adjustments needed)
  - Safe migration for new indexes
  - Production database schema reconciliation
  - Automated tests and verification
- **OUT**:
  - Dropping tables or resetting database
  - Modifying previously applied migrations (`InitialTables`, `AddBlockUsersTable`, `InitialUpdateBlockUserTable`, `AddAuditLogsTable`)
  - Changing API contracts or endpoints

## Dependencies
- Pomelo.EntityFrameworkCore.MySql
- MySQL Server (production instance configured in environment)
- ASP.NET Core 9 / EF Core 9 CLI

## Complexity
Medium (Safety-critical due to production database and MySQL foreign key constraints)
