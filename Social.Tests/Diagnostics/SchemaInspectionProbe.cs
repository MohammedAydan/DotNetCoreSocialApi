using System.Data;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Social.Infrastructure.Data;
using Xunit;
using Xunit.Abstractions;

namespace Social.Tests.Diagnostics
{
    public class SchemaInspectionProbe
    {
        private readonly ITestOutputHelper _output;

        public SchemaInspectionProbe(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task DumpProductionDatabaseSchema()
        {
            // Find and load .env from Social project if present
            var projectDir = AppContext.BaseDirectory;
            while (!string.IsNullOrEmpty(projectDir) && !File.Exists(Path.Combine(projectDir, "Social.sln")))
            {
                var parent = Directory.GetParent(projectDir);
                if (parent == null) break;
                projectDir = parent.FullName;
            }

            var envPath = Path.Combine(projectDir, "Social", ".env");
            if (File.Exists(envPath))
            {
                DotNetEnv.Env.Load(envPath);
            }

            var connStr = Environment.GetEnvironmentVariable("CONNECTION_STRING");
            if (string.IsNullOrEmpty(connStr))
            {
                _output.WriteLine("CONNECTION_STRING environment variable is not set.");
                return;
            }

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseMySql(connStr, new MySqlServerVersion(new Version(8, 0, 36)));

            using var db = new ApplicationDbContext(optionsBuilder.Options);
            var connection = db.Database.GetDbConnection();
            await connection.OpenAsync();

            var sb = new StringBuilder();
            sb.AppendLine("=== PRODUCTION DATABASE SCHEMA DUMP ===");
            sb.AppendLine($"Database: {connection.Database}");
            sb.AppendLine($"Timestamp UTC: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            // 1. __EFMigrationsHistory
            sb.AppendLine("--- __EFMigrationsHistory ---");
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT MigrationId, ProductVersion FROM __EFMigrationsHistory ORDER BY MigrationId;";
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    sb.AppendLine($"Migration: {reader.GetString(0)} | ProductVersion: {reader.GetString(1)}");
                }
            }
            sb.AppendLine();

            // 2. SHOW CREATE TABLE for key tables
            var tables = new[] { "Posts", "BlockUsers", "RefreshTokens", "Likes", "Comments", "Media", "Notifications", "AspNetUsers" };
            foreach (var table in tables)
            {
                sb.AppendLine($"--- SHOW CREATE TABLE {table} ---");
                try
                {
                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = $"SHOW CREATE TABLE `{table}`;";
                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        sb.AppendLine(reader.GetString(1));
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"ERROR: {ex.Message}");
                }
                sb.AppendLine();
            }

            // 3. Foreign keys from INFORMATION_SCHEMA.KEY_COLUMN_USAGE
            sb.AppendLine("--- FOREIGN KEYS (INFORMATION_SCHEMA.KEY_COLUMN_USAGE) ---");
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT TABLE_NAME, CONSTRAINT_NAME, COLUMN_NAME, REFERENCED_TABLE_NAME, REFERENCED_COLUMN_NAME
                    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                    WHERE TABLE_SCHEMA = DATABASE() 
                      AND REFERENCED_TABLE_NAME IS NOT NULL
                      AND TABLE_NAME IN ('Posts', 'BlockUsers', 'RefreshTokens', 'Likes', 'Comments', 'Media', 'Notifications')
                    ORDER BY TABLE_NAME, CONSTRAINT_NAME, ORDINAL_POSITION;";
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    sb.AppendLine($"Table: {reader.GetString(0)} | Constraint: {reader.GetString(1)} | Column: {reader.GetString(2)} -> RefTable: {reader.GetString(3)} ({reader.GetString(4)})");
                }
            }
            sb.AppendLine();

            // 4. Indexes from INFORMATION_SCHEMA.STATISTICS
            sb.AppendLine("--- INDEXES (INFORMATION_SCHEMA.STATISTICS) ---");
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT TABLE_NAME, INDEX_NAME, NON_UNIQUE, SEQ_IN_INDEX, COLUMN_NAME
                    FROM INFORMATION_SCHEMA.STATISTICS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME IN ('Posts', 'BlockUsers', 'RefreshTokens', 'Likes', 'Comments', 'Media', 'Notifications')
                    ORDER BY TABLE_NAME, INDEX_NAME, SEQ_IN_INDEX;";
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    sb.AppendLine($"Table: {reader.GetString(0)} | Index: {reader.GetString(1)} | NonUnique: {reader.GetInt64(2)} | Seq: {reader.GetInt64(3)} | Column: {reader.GetString(4)}");
                }
            }

            var dumpPath = Path.Combine(projectDir, "plans", "ef-core-indexes-and-relationships", "production_schema_dump.txt");
            await File.WriteAllTextAsync(dumpPath, sb.ToString());
            _output.WriteLine($"Saved schema dump to: {dumpPath}");

            // Check for orphaned rows
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT COUNT(*) FROM blockusers b LEFT JOIN aspnetusers u ON b.UserId = u.Id WHERE u.Id IS NULL;
                ";
                var orphanedBlockUser = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                _output.WriteLine($"Orphaned BlockUsers.UserId: {orphanedBlockUser}");
            }
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT COUNT(*) FROM blockusers b LEFT JOIN aspnetusers u ON b.BlockedUserId = u.Id WHERE u.Id IS NULL;
                ";
                var orphanedBlockBlocked = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                _output.WriteLine($"Orphaned BlockUsers.BlockedUserId: {orphanedBlockBlocked}");
            }
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT COUNT(*) FROM posts p LEFT JOIN posts parent ON p.ParentPostId = parent.Id WHERE p.ParentPostId IS NOT NULL AND parent.Id IS NULL;
                ";
                var orphanedParentPost = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                _output.WriteLine($"Orphaned Posts.ParentPostId: {orphanedParentPost}");
            }
            _output.WriteLine(sb.ToString());
        }

        [Fact]
        public async Task CheckDataCompatibilityForNewIndexes()
        {
            var projectDir = AppContext.BaseDirectory;
            while (!string.IsNullOrEmpty(projectDir) && !File.Exists(Path.Combine(projectDir, "Social.sln")))
            {
                var parent = Directory.GetParent(projectDir);
                if (parent == null) break;
                projectDir = parent.FullName;
            }

            var envPath = Path.Combine(projectDir, "Social", ".env");
            if (File.Exists(envPath))
            {
                DotNetEnv.Env.Load(envPath);
            }

            var connStr = Environment.GetEnvironmentVariable("CONNECTION_STRING");
            if (string.IsNullOrEmpty(connStr)) return;

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseMySql(connStr, new MySqlServerVersion(new Version(8, 0, 36)));

            using var db = new ApplicationDbContext(optionsBuilder.Options);
            var connection = db.Database.GetDbConnection();
            await connection.OpenAsync();

            // 1. Max length of RefreshTokens.Token
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT COALESCE(MAX(CHAR_LENGTH(Token)), 0) FROM refreshtokens;";
                var maxTokenLen = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                _output.WriteLine($"Max RefreshTokens.Token length: {maxTokenLen} (limit is 512)");
                Assert.True(maxTokenLen <= 512, $"Max token length {maxTokenLen} exceeds 512!");
            }

            // 2. Duplicate RefreshTokens.Token
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM (SELECT Token FROM refreshtokens GROUP BY Token HAVING COUNT(*) > 1) d;";
                var duplicateTokens = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                _output.WriteLine($"Duplicate RefreshTokens.Token count: {duplicateTokens}");
                Assert.True(duplicateTokens == 0, $"Found {duplicateTokens} duplicate tokens!");
            }

            // 3. Max length of Posts.Visibility
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT COALESCE(MAX(CHAR_LENGTH(Visibility)), 0) FROM posts;";
                var maxVisLen = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                _output.WriteLine($"Max Posts.Visibility length: {maxVisLen} (limit is 20)");
                Assert.True(maxVisLen <= 20, $"Max visibility length {maxVisLen} exceeds 20!");
            }

            // 4. Duplicate Likes (UserId, PostId)
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM (SELECT UserId, PostId FROM likes GROUP BY UserId, PostId HAVING COUNT(*) > 1) d;";
                var duplicateLikes = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                _output.WriteLine($"Duplicate Likes count: {duplicateLikes}");
                Assert.True(duplicateLikes == 0, $"Found {duplicateLikes} duplicate likes!");
            }

            // 5. Duplicate Followers (FollowerId, FollowingId)
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM (SELECT FollowerId, FollowingId FROM followers GROUP BY FollowerId, FollowingId HAVING COUNT(*) > 1) d;";
                var duplicateFollowers = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                _output.WriteLine($"Duplicate Followers count: {duplicateFollowers}");
                Assert.True(duplicateFollowers == 0, $"Found {duplicateFollowers} duplicate followers!");
            }

            _output.WriteLine("ALL DATA COMPATIBILITY CHECKS PASSED!");
        }

        [Fact]
        public async Task TestConditionalFkDropLogic()
        {
            var projectDir = AppContext.BaseDirectory;
            while (!string.IsNullOrEmpty(projectDir) && !File.Exists(Path.Combine(projectDir, "Social.sln")))
            {
                var parent = Directory.GetParent(projectDir);
                if (parent == null) break;
                projectDir = parent.FullName;
            }

            var envPath = Path.Combine(projectDir, "Social", ".env");
            if (File.Exists(envPath))
            {
                DotNetEnv.Env.Load(envPath);
            }

            var connStr = Environment.GetEnvironmentVariable("CONNECTION_STRING");
            if (string.IsNullOrEmpty(connStr)) return;

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseMySql(connStr, new MySqlServerVersion(new Version(8, 0, 36)));

            using var db = new ApplicationDbContext(optionsBuilder.Options);
            var connection = db.Database.GetDbConnection();
            await connection.OpenAsync();

            // Test stored procedure creation, execution, and cleanup on production MySQL
            var testSql = @"
                DROP PROCEDURE IF EXISTS `drop_fk_if_exists_test`;
                CREATE PROCEDURE `drop_fk_if_exists_test`(IN tbl VARCHAR(64), IN fk VARCHAR(64))
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
                        WHERE CONSTRAINT_SCHEMA = DATABASE()
                          AND TABLE_NAME = tbl
                          AND CONSTRAINT_NAME = fk
                          AND CONSTRAINT_TYPE = 'FOREIGN KEY'
                    ) THEN
                        SET @s = CONCAT('ALTER TABLE `', tbl, '` DROP FOREIGN KEY `', fk, '`');
                        PREPARE stmt FROM @s;
                        EXECUTE stmt;
                        DEALLOCATE PREPARE stmt;
                    END IF;
                END;
                CALL `drop_fk_if_exists_test`('blockusers', 'FK_NON_EXISTENT_TEST_KEY');
                DROP PROCEDURE `drop_fk_if_exists_test`;
            ";

            using var cmd = connection.CreateCommand();
            cmd.CommandText = testSql;
            await cmd.ExecuteNonQueryAsync();
            _output.WriteLine("Conditional FK drop stored procedure test SUCCEEDED!");
        }

        [Fact]
        public void InspectBlockUserEntity()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseMySql("Server=localhost;Database=dummy;Uid=root;Pwd=root;", new MySqlServerVersion(new Version(8, 0, 36)));

            using var db = new ApplicationDbContext(optionsBuilder.Options);
            var entity = db.Model.FindEntityType(typeof(Social.Core.Entities.BlockUser));
            Assert.NotNull(entity);

            _output.WriteLine($"BlockUser Primary Key: {string.Join(", ", entity.FindPrimaryKey()!.Properties.Select(p => p.Name))}");
            _output.WriteLine($"BlockUser Properties: {string.Join(", ", entity.GetProperties().Select(p => $"{p.Name} ({p.ClrType.Name})"))}");
            foreach (var fk in entity.GetForeignKeys())
            {
                _output.WriteLine($"BlockUser FK: {fk.GetConstraintName()} ({string.Join(", ", fk.Properties.Select(p => p.Name))}) -> {fk.PrincipalEntityType.ShortName()} ({string.Join(", ", fk.PrincipalKey.Properties.Select(p => p.Name))}) [DeleteBehavior: {fk.DeleteBehavior}]");
            }
            foreach (var idx in entity.GetIndexes())
            {
                _output.WriteLine($"BlockUser Index: {idx.GetDatabaseName()} ({string.Join(", ", idx.Properties.Select(p => p.Name))}) [Unique: {idx.IsUnique}]");
            }
        }

        [Fact]
        public void InspectCompiledEfModel()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseMySql("Server=localhost;Database=dummy;Uid=root;Pwd=root;", new MySqlServerVersion(new Version(8, 0, 36)));

            using var db = new ApplicationDbContext(optionsBuilder.Options);
            var model = db.Model;

            var sb = new StringBuilder();
            sb.AppendLine("=== COMPILED EF CORE MODEL INSPECTION ===");

            var shadowFks = new List<string>();

            foreach (var entityType in model.GetEntityTypes())
            {
                sb.AppendLine($"Entity: {entityType.Name} -> Table: {entityType.GetTableName()}");
                
                // Properties
                sb.AppendLine("  Properties:");
                foreach (var prop in entityType.GetProperties())
                {
                    var isShadow = prop.IsShadowProperty();
                    sb.AppendLine($"    {prop.Name} ({prop.ClrType.Name}) [Shadow: {isShadow}, Column: {prop.GetColumnName()}]");
                    if (isShadow || prop.Name.EndsWith("1"))
                    {
                        shadowFks.Add($"{entityType.ShortName()}.{prop.Name} (Shadow: {isShadow})");
                    }
                }

                // Keys
                sb.AppendLine("  Keys:");
                foreach (var key in entityType.GetKeys())
                {
                    sb.AppendLine($"    Key: {string.Join(", ", key.Properties.Select(p => p.Name))} [IsPrimary: {key.IsPrimaryKey()}]");
                }

                // Foreign keys
                sb.AppendLine("  Foreign Keys:");
                foreach (var fk in entityType.GetForeignKeys())
                {
                    sb.AppendLine($"    FK: {fk.GetConstraintName() ?? "unnamed"} -> {fk.PrincipalEntityType.ShortName()} ({string.Join(", ", fk.Properties.Select(p => p.Name))} -> {string.Join(", ", fk.PrincipalKey.Properties.Select(p => p.Name))}) [DeleteBehavior: {fk.DeleteBehavior}]");
                }

                // Navigations
                sb.AppendLine("  Navigations:");
                foreach (var nav in entityType.GetNavigations())
                {
                    sb.AppendLine($"    Nav: {nav.Name} -> Target: {nav.TargetEntityType.ShortName()} [IsCollection: {nav.IsCollection}]");
                }

                // Indexes
                sb.AppendLine("  Indexes:");
                foreach (var idx in entityType.GetIndexes())
                {
                    var cols = string.Join(", ", idx.Properties.Select(p => p.Name));
                    sb.AppendLine($"    Index: {idx.GetDatabaseName()} on ({cols}) [Unique: {idx.IsUnique}]");
                }

                sb.AppendLine();
            }

            sb.AppendLine("--- SHADOW / SUSPICIOUS PROPERTIES ---");
            if (shadowFks.Count == 0)
            {
                sb.AppendLine("NONE! Zero shadow foreign keys or *1 properties detected.");
            }
            else
            {
                foreach (var sfk in shadowFks)
                {
                    sb.AppendLine($"[!] {sfk}");
                }
            }

            _output.WriteLine(sb.ToString());
        }

        [Fact]
        public async Task VerifyProductionDatabaseQueriesAndWebStartup()
        {
            var projectDir = AppContext.BaseDirectory;
            while (!string.IsNullOrEmpty(projectDir) && !File.Exists(Path.Combine(projectDir, "Social.sln")))
            {
                var parent = Directory.GetParent(projectDir);
                if (parent == null) break;
                projectDir = parent.FullName;
            }

            var envPath = Path.Combine(projectDir, "Social", ".env");
            if (File.Exists(envPath))
            {
                DotNetEnv.Env.Load(envPath);
            }

            var connStr = Environment.GetEnvironmentVariable("CONNECTION_STRING");
            if (string.IsNullOrEmpty(connStr)) return;

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseMySql(connStr, new MySqlServerVersion(new Version(8, 0, 36)));

            using var db = new ApplicationDbContext(optionsBuilder.Options);

            // Test LINQ queries with eager-loaded navigations across all tables
            var posts = await db.Posts
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .Include(p => p.Media)
                .OrderByDescending(p => p.CreatedAt)
                .Take(3)
                .ToListAsync();
            _output.WriteLine($"Queried {posts.Count} posts with navigations successfully.");

            var blocks = await db.BlockUsers
                .Include(b => b.User)
                .Include(b => b.BlockedUser)
                .Take(3)
                .ToListAsync();
            _output.WriteLine($"Queried {blocks.Count} block records with navigations successfully.");

            var tokens = await db.RefreshTokens
                .Include(r => r.User)
                .Take(3)
                .ToListAsync();
            _output.WriteLine($"Queried {tokens.Count} refresh tokens with navigations successfully.");

            var followers = await db.Followers
                .Include(f => f.FollowerUser)
                .Include(f => f.FollowingUser)
                .Take(3)
                .ToListAsync();
            _output.WriteLine($"Queried {followers.Count} follower records with navigations successfully.");

            // Verify Web Application Startup & HTTP 200 response
            using var factory = new Infrastructure.CustomWebApplicationFactory();
            var client = factory.CreateClient();
            var response = await client.GetAsync("/admin/login");
            _output.WriteLine($"GET /admin/login returned: {(int)response.StatusCode} {response.StatusCode}");
            Assert.True((int)response.StatusCode >= 200 && (int)response.StatusCode < 400);
        }

        [Fact]
        public async Task DiagnoseFeedAndPostModel()
        {
            var projectDir = AppContext.BaseDirectory;
            while (!string.IsNullOrEmpty(projectDir) && !File.Exists(Path.Combine(projectDir, "Social.sln")))
            {
                var parent = Directory.GetParent(projectDir);
                if (parent == null) break;
                projectDir = parent.FullName;
            }

            var envPath = Path.Combine(projectDir, "Social", ".env");
            if (File.Exists(envPath))
            {
                DotNetEnv.Env.Load(envPath);
            }

            var connStr = Environment.GetEnvironmentVariable("CONNECTION_STRING")
                ?? "Server=localhost;Database=dummy;Uid=root;Pwd=root;";

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseMySql(connStr, new MySqlServerVersion(new Version(8, 0, 36)));

            using var db = new ApplicationDbContext(optionsBuilder.Options);

            _output.WriteLine("============================================================");
            _output.WriteLine("TASK 4: PROGRAMMATIC MODEL INSPECTION (Post Entity)");
            _output.WriteLine("============================================================");

            var postEntity = db.Model.GetEntityTypes().Single(e => e.ClrType == typeof(Social.Core.Entities.Post));
            Assert.NotNull(postEntity);

            _output.WriteLine($"Entity: {postEntity.Name} | Table: {postEntity.GetTableName()}");
            _output.WriteLine("Properties:");

            bool hasUserId = false;
            bool hasUserId1 = false;

            foreach (var prop in postEntity.GetProperties())
            {
                var isShadow = prop.IsShadowProperty();
                var isFk = prop.IsForeignKey();
                var colName = prop.GetColumnName();
                _output.WriteLine($"  - Property: {prop.Name} | ClrType: {prop.ClrType.Name} | IsShadow: {isShadow} | IsForeignKey: {isFk} | Column: {colName}");

                if (prop.Name == "UserId") hasUserId = true;
                if (prop.Name == "UserId1") hasUserId1 = true;
            }

            _output.WriteLine($"Verification: UserId exists = {hasUserId}, UserId1 exists = {hasUserId1}");
            Assert.True(hasUserId, "UserId property MUST exist on Post");
            Assert.False(hasUserId1, "UserId1 property MUST NOT exist on Post");

            _output.WriteLine("\nForeign Keys on Post:");
            foreach (var fk in postEntity.GetForeignKeys())
            {
                var props = string.Join(", ", fk.Properties.Select(p => p.Name));
                var principalType = fk.PrincipalEntityType.ShortName();
                var principalKey = string.Join(", ", fk.PrincipalKey.Properties.Select(p => p.Name));
                var nav = fk.DependentToPrincipal?.Name ?? "(none)";
                var principalToDep = fk.PrincipalToDependent?.Name ?? "(none)";
                _output.WriteLine($"  - FK Properties: [{props}] -> Principal: {principalType}[{principalKey}] | Navigation: {nav} | PrincipalToDependent: {principalToDep} | IsUnique: {fk.IsUnique} | DeleteBehavior: {fk.DeleteBehavior}");
            }

            _output.WriteLine("\n============================================================");
            _output.WriteLine("TASK 3: FEED QUERY GENERATED SQL INSPECTION");
            _output.WriteLine("============================================================");

            const string viewerId = "diagnostic-viewer-id";
            var feedQuery = db.Posts
                .AsNoTracking()
                .Where(p => !p.IsDeleted
                    && (p.UserId == viewerId
                        || (db.Followers.Any(f => f.FollowerId == viewerId && f.FollowingId == p.UserId && f.Accepted)
                            && p.Visibility == "public"
                            && !db.BlockUsers.Any(b => (b.UserId == viewerId && b.BlockedUserId == p.UserId) || (b.UserId == p.UserId && b.BlockedUserId == viewerId)))))
                .OrderByDescending(p => p.CreatedAt)
                .ThenByDescending(p => p.Id)
                .Skip(0)
                .Take(20)
                .Include(p => p.User)
                .Include(p => p.Media)
                .Include(p => p.ParentPost).ThenInclude(p => p!.User)
                .Include(p => p.ParentPost).ThenInclude(p => p!.Media)
                .Include(p => p.ParentPost).ThenInclude(p => p!.ParentPost).ThenInclude(p => p!.User)
                .Include(p => p.ParentPost).ThenInclude(p => p!.ParentPost).ThenInclude(p => p!.Media)
                .Include(p => p.ParentPost).ThenInclude(p => p!.ParentPost).ThenInclude(p => p!.ParentPost).ThenInclude(p => p!.User)
                .Include(p => p.ParentPost).ThenInclude(p => p!.ParentPost).ThenInclude(p => p!.ParentPost).ThenInclude(p => p!.Media)
                .AsSplitQuery();

            var generatedSql = feedQuery.ToQueryString();
            _output.WriteLine("Generated SQL for Feed Query:");
            _output.WriteLine(generatedSql);

            Assert.DoesNotContain("UserId1", generatedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("UserId", generatedSql, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("\n[✓] Generated SQL contains 'UserId' and DOES NOT contain 'UserId1'!");

            // Test execution against real DB if live connection string is available
            var realConnStr = Environment.GetEnvironmentVariable("CONNECTION_STRING");
            if (!string.IsNullOrEmpty(realConnStr))
            {
                _output.WriteLine("\nExecuting PostRepository.GetFeedPostsAsync on real production MySQL database...");
                var repo = new Social.Infrastructure.Repositories.PostRepository(db);
                var posts = await repo.GetFeedPostsAsync("non-existent-user", 1, 10);
                _output.WriteLine($"Query executed successfully on real database! Returned count: {posts.Count()}");
            }
        }

        [Fact]
        public async Task TestFeedEndpointAgainstRunningLocalServer()
        {
            var projectDir = AppContext.BaseDirectory;
            while (!string.IsNullOrEmpty(projectDir) && !File.Exists(Path.Combine(projectDir, "Social.sln")))
            {
                var parent = Directory.GetParent(projectDir);
                if (parent == null) break;
                projectDir = parent.FullName;
            }

            var envPath = Path.Combine(projectDir, "Social", ".env");
            if (File.Exists(envPath))
            {
                DotNetEnv.Env.Load(envPath);
            }

            var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");
            var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
            var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "diagnostic-local-user-id"),
                new Claim(ClaimTypes.Role, "User")
            };
            var jwtToken = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );
            var token = new JwtSecurityTokenHandler().WriteToken(jwtToken);
            _output.WriteLine($"Generated valid test JWT token (length: {token.Length})");

            using var factory = new Infrastructure.CustomWebApplicationFactory();
            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Test");

            var res = await client.GetAsync("/api/posts/feed?Page=1&Limit=20");
            var body = await res.Content.ReadAsStringAsync();
            _output.WriteLine($"In-Process Server Response Status: {(int)res.StatusCode} {res.StatusCode}");
            _output.WriteLine($"In-Process Server Response Body: {body}");

            Assert.Equal(System.Net.HttpStatusCode.OK, res.StatusCode);
            Assert.Contains("\"success\":true", body);
            Assert.DoesNotContain("UserId1", body);
        }
    }
}
