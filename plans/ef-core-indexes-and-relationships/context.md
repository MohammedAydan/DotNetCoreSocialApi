# Context: EF Core Indexes & Relationship Normalization

## Files to touch / inspect
- `Social.Infrastructure/Data/ApplicationDbContext.cs`
- `Social.Core/Entities/User.cs`
- `Social.Core/Entities/Post.cs`
- `Social.Core/Entities/Like.cs`
- `Social.Core/Entities/Comment.cs`
- `Social.Core/Entities/Media.cs`
- `Social.Core/Entities/RefreshToken.cs`
- `Social.Core/Entities/Follower.cs`
- `Social.Core/Entities/BlockUser.cs`
- `Social.Infrastructure/Migrations/*`
- `Social.Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs`

## Dependencies
- Pomelo.EntityFrameworkCore.MySql
- Microsoft.EntityFrameworkCore.Design / Tools

## Env Vars & Config
- Database connection strings in appsettings / .env / environment variables.

## Known Prior Migrations
- `20260208224707_InitialTables`
- `20260209000001_AddBlockUsersTable`
- `20260209000225_InitialUpdateBlockUserTable`
- `20260915011757_AddAuditLogsTable`
