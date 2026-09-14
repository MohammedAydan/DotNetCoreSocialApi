# Database Schema Documentation

This document provides an overview of the database schema, relationships, and migration management for the Social API.

## Database Provider

- **Provider**: MySQL
- **ORM**: Entity Framework Core
- **Connection**: Configured via `CONNECTION_STRING` environment variable

## Entity Relationship Diagram

```
User (AspNetUsers - Identity)
  |
  ├──> Posts (1:N)
  ├──> Comments (1:N)
  ├──> Likes (1:N)
  ├──> Notifications (1:N - receiver)
  ├──> BlockUser (1:N - blocker)
  ├──> BlockUser (1:N - blocked)
  ├──> Follower (1:N - follower)
  ├──> Follower (1:N - following)
  └──> RefreshToken (1:N)

Post
  |
  ├──> Comments (1:N)
  ├──> Likes (1:N)
  ├──> Media (1:N)
  └──> Notifications (1:N)

Comment
  ├──> Likes (1:N)
  └──> Notifications (1:N)
```

## Core Entities

### 1. User (ASP.NET Identity)
Extends `IdentityUser` for authentication and authorization.

**Key Fields**:
- `Id` (string, PK) - User identifier
- `UserName` (string) - Unique username
- `Email` (string) - User email
- `PasswordHash` (string) - Hashed password
- `PhoneNumber` (string) - Optional phone
- Standard Identity fields (EmailConfirmed, etc.)

**Relationships**:
- Posts, Comments, Likes, Notifications, Followers, RefreshTokens, BlockedUsers

### 2. Post
User-generated content posts.

**Key Fields**:
- `Id` (Guid, PK) - Post identifier
- `UserId` (string, FK) - Post author
- `Content` (string) - Post text content
- `CreatedAt` (DateTime) - Creation timestamp
- `UpdatedAt` (DateTime) - Last update timestamp
- `LikeCount` (int) - Cached like count
- `CommentCount` (int) - Cached comment count

**Relationships**:
- User (N:1), Comments (1:N), Likes (1:N), Media (1:N)

### 3. Comment
Comments on posts.

**Key Fields**:
- `Id` (Guid, PK) - Comment identifier
- `PostId` (Guid, FK) - Parent post
- `UserId` (string, FK) - Comment author
- `Content` (string) - Comment text
- `CreatedAt` (DateTime) - Creation timestamp
- `LikeCount` (int) - Cached like count

**Relationships**:
- Post (N:1), User (N:1), Likes (1:N)

### 4. Like
Likes on posts or comments.

**Key Fields**:
- `Id` (Guid, PK) - Like identifier
- `UserId` (string, FK) - User who liked
- `PostId` (Guid?, FK) - Liked post (nullable if comment)
- `CommentId` (Guid?, FK) - Liked comment (nullable if post)
- `CreatedAt` (DateTime) - Like timestamp

**Constraints**:
- Either `PostId` or `CommentId` must be set (not both)

**Relationships**:
- User (N:1), Post (N:1), Comment (N:1)

### 5. Follower
User following relationships.

**Key Fields**:
- `Id` (Guid, PK) - Relationship identifier
- `FollowerId` (string, FK) - User doing the following
- `FollowingId` (string, FK) - User being followed
- `CreatedAt` (DateTime) - Follow timestamp

**Constraints**:
- Unique constraint on (FollowerId, FollowingId)
- Cannot follow self

**Relationships**:
- Follower User (N:1), Following User (N:1)

### 6. Notification
User notifications for various events.

**Key Fields**:
- `Id` (Guid, PK) - Notification identifier
- `UserId` (string, FK) - Notification recipient
- `Type` (string) - Notification type (Like, Comment, Follow, etc.)
- `Content` (string) - Notification message
- `IsRead` (bool) - Read status
- `ReferenceId` (string) - Entity that triggered notification
- `CreatedAt` (DateTime) - Notification timestamp

**Types**:
- `Like` - Someone liked your content
- `Comment` - Someone commented on your post
- `Follow` - Someone followed you
- `NewPost` - Someone you follow posted

**Relationships**:
- User (N:1)

### 7. BlockUser
User blocking relationships.

**Key Fields**:
- `Id` (Guid, PK) - Block relationship identifier
- `UserId` (string, FK) - User doing the blocking
- `BlockedUserId` (string, FK) - User being blocked
- `CreatedAt` (DateTime) - Block timestamp

**Constraints**:
- Unique constraint on (UserId, BlockedUserId)
- Cannot block self (enforced in application logic)

**Relationships**:
- Blocker User (N:1), Blocked User (N:1)

### 8. Media
Media attachments for posts.

**Key Fields**:
- `Id` (Guid, PK) - Media identifier
- `PostId` (Guid, FK) - Parent post
- `Url` (string) - Media URL
- `Type` (string) - Media type (Image, Video)
- `CreatedAt` (DateTime) - Upload timestamp

**Relationships**:
- Post (N:1)

### 9. RefreshToken
JWT refresh tokens for authentication.

**Key Fields**:
- `Id` (Guid, PK) - Token identifier
- `UserId` (string, FK) - Token owner
- `Token` (string) - Refresh token value
- `ExpiresAt` (DateTime) - Token expiration
- `CreatedAt` (DateTime) - Creation timestamp
- `RevokedAt` (DateTime?) - Revocation timestamp (nullable)

**Relationships**:
- User (N:1)

### 10. EmailSettings
Email configuration settings.

**Key Fields**:
- `SmtpServer` (string) - SMTP server hostname
- `SmtpPort` (int) - SMTP port
- `SenderName` (string) - Sender display name
- `SenderEmail` (string) - Sender email address
- `Username` (string) - SMTP username
- `Password` (string) - SMTP password
- `EnableSSL` (bool) - SSL enabled flag

### 11. GenralConfig
General application configuration.

**Note**: Typo in name (`Genral` should be `General`) - requires migration to fix.

**Key Fields**:
- `FrontendUrl` (string) - Frontend application URL

### 12. SignIn
Login attempt tracking (for rate limiting/security).

**Key Fields**:
- `Id` (Guid, PK) - Sign-in identifier
- `Email` (string) - Email used for sign-in
- `AttemptedAt` (DateTime) - Attempt timestamp
- `Success` (bool) - Whether sign-in succeeded
- `IpAddress` (string) - IP address of attempt

## Database Migrations

### Migration Commands

#### Create a new migration:
```bash
dotnet ef migrations add <MigrationName> --project Social.Infrastructure --startup-project Social
```

#### Apply migrations to database:
```bash
dotnet ef database update --project Social.Infrastructure --startup-project Social
```

#### Remove last migration (if not applied):
```bash
dotnet ef migrations remove --project Social.Infrastructure --startup-project Social
```

#### List all migrations:
```bash
dotnet ef migrations list --project Social.Infrastructure --startup-project Social
```

#### Generate SQL script:
```bash
dotnet ef migrations script --project Social.Infrastructure --startup-project Social --output migration.sql
```

### Migration History

#### Initial Migration
**Name**: `20250827065419_Init`
**Purpose**: Initial database schema with core entities

**Entities Created**:
- User (via Identity)
- Post, Comment, Like
- Follower, Notification
- Media, RefreshToken
- EmailSettings, GenralConfig, SignIn

**Indexes Created**:
- Posts: Index on UserId, CreatedAt
- Comments: Index on PostId, UserId
- Likes: Index on PostId, CommentId, UserId
- Followers: Composite unique index on (FollowerId, FollowingId)

**Relationships Configured**:
- Cascade deletes configured for parent-child relationships
- Restrict deletes configured to prevent accidental data loss on shared entities

**Future Migrations** (if BlockUser was added later):
- Add BlockUser table
- Add indexes on (UserId, BlockedUserId)
- Add unique constraint

### Best Practices for Migrations

1. **Always backup production database** before applying migrations

2. **Test migrations in development first**:
   ```bash
   # Development
   dotnet ef database update --project Social.Infrastructure --startup-project Social
   ```

3. **Use descriptive migration names**:
   ```bash
   # Good
   dotnet ef migrations add AddBlockUserFeature

   # Bad
   dotnet ef migrations add Update1
   ```

4. **Review generated migration code** before applying:
   - Check `Up()` method for forward migration
   - Check `Down()` method for rollback
   - Verify data types and constraints

5. **Document breaking changes** in migration comments:
   ```csharp
   /// <summary>
   /// Adds BlockUser entity to support user blocking functionality.
   /// BREAKING: Requires cache invalidation after deployment.
   /// </summary>
   public partial class AddBlockUserEntity : Migration
   {
       protected override void Up(MigrationBuilder migrationBuilder)
       {
           // Migration code
       }
   }
   ```

6. **Handle data migrations separately**:
   ```csharp
   protected override void Up(MigrationBuilder migrationBuilder)
   {
       // Schema change
       migrationBuilder.AddColumn<string>("NewColumn", "Users");

       // Data migration (if simple)
       migrationBuilder.Sql("UPDATE Users SET NewColumn = OldColumn");

       // Drop old column
       migrationBuilder.DropColumn("OldColumn", "Users");
   }
   ```

7. **Use migrations to seed reference data**:
   ```csharp
   protected override void Up(MigrationBuilder migrationBuilder)
   {
       migrationBuilder.Sql(@"
           INSERT INTO GenralConfig (FrontendUrl)
           VALUES ('https://social.example.com')
       ");
   }
   ```

## Schema Validation

### Indexes
Verify the following indexes exist for performance:

- `IX_Posts_UserId` - Post queries by user
- `IX_Posts_CreatedAt` - Post ordering by date
- `IX_Comments_PostId` - Comments by post
- `IX_Comments_UserId` - Comments by user
- `IX_Likes_PostId` - Likes by post
- `IX_Likes_CommentId` - Likes by comment
- `IX_Likes_UserId` - Likes by user
- `IX_Followers_FollowerId` - Following relationships
- `IX_Followers_FollowingId` - Follower relationships
- `IX_Notifications_UserId_IsRead` - Unread notifications query
- `IX_BlockUser_UserId_BlockedUserId` - Block checks

### Constraints
Verify these constraints are enforced:

- Primary keys on all entities
- Foreign keys with appropriate cascade/restrict rules
- Unique constraints on (FollowerId, FollowingId)
- Unique constraints on (UserId, BlockedUserId)
- Check constraints for self-referential relationships

### Data Integrity Checks

```sql
-- Check for orphaned posts (user deleted but posts remain)
SELECT p.* FROM Posts p
LEFT JOIN AspNetUsers u ON p.UserId = u.Id
WHERE u.Id IS NULL;

-- Check for likes without valid post or comment
SELECT l.* FROM Likes l
LEFT JOIN Posts p ON l.PostId = p.Id
LEFT JOIN Comments c ON l.CommentId = c.Id
WHERE (l.PostId IS NOT NULL AND p.Id IS NULL)
   OR (l.CommentId IS NOT NULL AND c.Id IS NULL);

-- Check for self-follow relationships
SELECT * FROM Follower
WHERE FollowerId = FollowingId;

-- Check for self-block relationships
SELECT * FROM BlockUser
WHERE UserId = BlockedUserId;
```

## Connection String Format

### Development (Local MySQL)
```
Server=localhost;Port=3306;Database=SocialDb;User=root;Password=your_password;
```

### Production (With SSL)
```
Server=prod-server.mysql.database.azure.com;Port=3306;Database=SocialDb;User=admin@prod-server;Password=your_password;SslMode=Required;
```

### Environment Variable
```bash
export CONNECTION_STRING="Server=localhost;Port=3306;Database=SocialDb;User=root;Password=your_password;"
```

### .env File
```
CONNECTION_STRING=Server=localhost;Port=3306;Database=SocialDb;User=root;Password=your_password;
```

## Troubleshooting

### Migration fails with "Table already exists"
```bash
# Remove last migration and recreate
dotnet ef migrations remove --project Social.Infrastructure --startup-project Social
dotnet ef migrations add FixedMigration --project Social.Infrastructure --startup-project Social
```

### Database out of sync with migrations
```bash
# Check migration history
dotnet ef migrations list --project Social.Infrastructure --startup-project Social

# If needed, manually update __EFMigrationsHistory table
```

### Connection string issues
```bash
# Test connection
mysql -h localhost -u root -p SocialDb

# Verify environment variable
echo $CONNECTION_STRING
```

---

*Last Updated: February 9, 2026*
*For schema change requests, contact the development team*
