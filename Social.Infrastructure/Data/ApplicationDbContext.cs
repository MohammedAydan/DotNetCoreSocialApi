using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Social.Core.Entities;

namespace Social.Infrastructure.Data
{
    public class ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : IdentityDbContext<User>(options)
    {
        public DbSet<Post> Posts { get; set; }
        public DbSet<Follower> Followers { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Media> Media { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<NotificationPreference> NotificationPreferences { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<BlockUser> BlockUsers { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<RequestLog> RequestLogs { get; set; }
        public DbSet<DailyMetricSnapshot> DailyMetricSnapshots { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =========================================================
            // 1. POST
            // =========================================================
            modelBuilder.Entity<Post>(entity =>
            {
                // Preserve single-column FK indexes needed in MySQL for foreign key constraints
                entity.HasIndex(p => p.UserId);
                entity.HasIndex(p => p.ParentPostId);

                // Feed ordering
                entity.HasIndex(p => new
                {
                    p.CreatedAt,
                    p.Id
                })
                .IsDescending(true, true);

                // User profile posts (covering: filter + deterministic order, no sort spill)
                entity.HasIndex(p => new
                {
                    p.UserId,
                    p.CreatedAt,
                    p.Id
                })
                .IsDescending(false, true, true);

                // Visibility filtering (covering: filter + deterministic order)
                entity.HasIndex(p => new
                {
                    p.Visibility,
                    p.CreatedAt,
                    p.Id
                })
                .IsDescending(false, true, true);

                entity.Property(p => p.Visibility)
                    .HasMaxLength(20);

                // Match AspNetUsers.Id (varchar(255)) to keep FK types aligned
                // and stay under MySQL utf8mb4 3072-byte index limit.
                entity.Property(p => p.UserId)
                    .HasMaxLength(255);

                entity.Property(p => p.ParentPostId)
                    .HasMaxLength(255);

                // NOTE: No global HasQueryFilter(p => !p.IsDeleted) here by design.
                // Admin moderation must list hidden/deleted posts; repositories
                // filter !IsDeleted explicitly per query for deterministic behavior.

                // IMPORTANT:
                // User.Posts exists, so explicitly configure the inverse.
                entity.HasOne(p => p.User)
                    .WithMany(u => u.Posts)
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Self-referencing parent post
                entity.HasOne(p => p.ParentPost)
                    .WithMany()
                    .HasForeignKey(p => p.ParentPostId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // =========================================================
            // 2. FOLLOWER
            // =========================================================
            modelBuilder.Entity<Follower>(entity =>
            {
                entity.HasKey(f => f.Id);

                entity.Property(f => f.FollowerId).HasMaxLength(255);
                entity.Property(f => f.FollowingId).HasMaxLength(255);

                // Preserve individual FK indexes
                entity.HasIndex(f => f.FollowerId);
                entity.HasIndex(f => f.FollowingId);

                entity.HasIndex(f => new
                {
                    f.FollowerId,
                    f.FollowingId
                })
                .IsUnique();

                entity.HasIndex(f => new
                {
                    f.FollowerId,
                    f.Accepted,
                    f.FollowingId
                });

                entity.HasIndex(f => new
                {
                    f.FollowingId,
                    f.Accepted
                });

                entity.HasOne(f => f.FollowerUser)
                    .WithMany(u => u.Following)
                    .HasForeignKey(f => f.FollowerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.FollowingUser)
                    .WithMany(u => u.Followers)
                    .HasForeignKey(f => f.FollowingId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // =========================================================
            // 3. LIKE
            // =========================================================
            modelBuilder.Entity<Like>(entity =>
            {
                entity.Property(l => l.UserId).HasMaxLength(255);
                entity.Property(l => l.PostId).HasMaxLength(255);

                // Preserve individual FK indexes
                entity.HasIndex(l => l.PostId);
                entity.HasIndex(l => l.UserId);

                entity.HasIndex(l => new
                {
                    l.UserId,
                    l.PostId
                })
                .IsUnique();

                // Explicit navigation -> prevents PostId1.
                entity.HasOne(l => l.Post)
                    .WithMany(p => p.Likes)
                    .HasForeignKey(l => l.PostId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Explicit User navigation.
                entity.HasOne(l => l.User)
                    .WithMany()
                    .HasForeignKey(l => l.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // =========================================================
            // 4. COMMENT
            // =========================================================
            modelBuilder.Entity<Comment>(entity =>
            {
                entity.Property(c => c.PostId).HasMaxLength(255);
                entity.Property(c => c.UserId).HasMaxLength(255);
                entity.Property(c => c.ParentId).HasMaxLength(255);

                // Preserve individual FK indexes
                entity.HasIndex(c => c.PostId);
                entity.HasIndex(c => c.UserId);
                entity.HasIndex(c => c.ParentId);

                entity.HasIndex(c => new
                {
                    c.PostId,
                    c.CreatedAt
                })
                .IsDescending(false, true);

                // Explicit navigation -> prevents PostId1.
                entity.HasOne(c => c.Post)
                    .WithMany(p => p.Comments)
                    .HasForeignKey(c => c.PostId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Explicit User navigation.
                entity.HasOne(c => c.User)
                    .WithMany()
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Self-referencing replies.
                entity.HasOne(c => c.Parent)
                    .WithMany(c => c.Replies)
                    .HasForeignKey(c => c.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // =========================================================
            // 5. MEDIA
            // =========================================================
            modelBuilder.Entity<Media>(entity =>
            {
                entity.Property(m => m.PostId).HasMaxLength(255);
                entity.Property(m => m.UserId).HasMaxLength(255);

                // Preserve individual FK indexes
                entity.HasIndex(m => m.PostId);
                entity.HasIndex(m => m.UserId);

                // Explicit navigation -> prevents PostId1.
                entity.HasOne(m => m.Post)
                    .WithMany(p => p.Media)
                    .HasForeignKey(m => m.PostId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Explicit User navigation.
                entity.HasOne(m => m.User)
                    .WithMany()
                    .HasForeignKey(m => m.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // =========================================================
            // 6. BLOCK USER
            // =========================================================
            modelBuilder.Entity<BlockUser>(entity =>
            {
                // Primary Key is Id (matches BlockUser.cs [Key] and production MySQL PRIMARY KEY)
                entity.HasKey(b => b.Id);

                entity.Property(b => b.Id).HasMaxLength(255);
                entity.Property(b => b.UserId).HasMaxLength(255);
                entity.Property(b => b.BlockedUserId).HasMaxLength(255);

                // Preserve individual FK indexes
                entity.HasIndex(b => b.UserId);
                entity.HasIndex(b => b.BlockedUserId);

                // Performance index for bidirectional block checks
                entity.HasIndex(b => new
                {
                    b.BlockedUserId,
                    b.UserId
                });

                entity.HasOne(b => b.User)
                    .WithMany()
                    .HasForeignKey(b => b.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(b => b.BlockedUser)
                    .WithMany()
                    .HasForeignKey(b => b.BlockedUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // =========================================================
            // 7. NOTIFICATION
            // =========================================================
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.Property(n => n.UserId).HasMaxLength(255);
                entity.Property(n => n.RecipientId).HasMaxLength(255);

                // Preserve individual FK indexes
                entity.HasIndex(n => n.UserId);
                entity.HasIndex(n => n.RecipientId);

                entity.HasIndex(n => new
                {
                    n.UserId,
                    n.IsRead,
                    n.CreatedAt
                })
                .IsDescending(false, false, true);

                // Smart inbox: recipient + visibility + priority + recency.
                entity.Property(n => n.GroupKey).HasMaxLength(300);
                entity.Property(n => n.LastActorName).HasMaxLength(120);
                entity.HasIndex(n => new
                {
                    n.RecipientId,
                    n.IsRead,
                    n.Priority,
                    n.CreatedAt
                })
                .IsDescending(false, false, true, true);

                // Aggregation lookups (surviving unread row per group).
                entity.HasIndex(n => new
                {
                    n.RecipientId,
                    n.GroupKey,
                    n.IsRead
                });

                entity.HasOne(n => n.RecipientUser)
                    .WithMany()
                    .HasForeignKey(n => n.RecipientId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // =========================================================
            // 7b. NOTIFICATION PREFERENCES (one row per user)
            // =========================================================
            modelBuilder.Entity<NotificationPreference>(entity =>
            {
                entity.HasKey(p => p.UserId);
                entity.Property(p => p.UserId).HasMaxLength(255);

                entity.HasOne(p => p.User)
                    .WithMany()
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // =========================================================
            // 7c. REQUEST TELEMETRY (append-only API request logs)
            // NOTE: Admin-action AuditLog is intentionally untouched (string
            // GUID PK, moderation semantics). Telemetry lives here in the
            // high-volume RequestLogs table with range-scan indexes.
            // =========================================================
            modelBuilder.Entity<RequestLog>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.Property(r => r.UserId).HasMaxLength(255);
                entity.Property(r => r.Endpoint).HasMaxLength(255).IsRequired();
                entity.Property(r => r.HttpMethod).HasMaxLength(10).IsRequired();
                entity.Property(r => r.IpAddress).HasMaxLength(45);
                entity.Property(r => r.UserAgent).HasMaxLength(512);

                // Time-range scans (retention + aggregation windows).
                entity.HasIndex(r => r.CreatedAt);
                entity.HasIndex(r => r.UserId);
                entity.HasIndex(r => r.Endpoint);
                entity.HasIndex(r => r.StatusCode);
                entity.HasIndex(r => new { r.CreatedAt, r.StatusCode });
                entity.HasIndex(r => new { r.Endpoint, r.CreatedAt });
            });

            // =========================================================
            // 7d. DAILY METRIC SNAPSHOTS (one row per UTC day)
            // =========================================================
            modelBuilder.Entity<DailyMetricSnapshot>(entity =>
            {
                entity.HasKey(s => s.Id);

                entity.HasIndex(s => s.Date).IsUnique();
            });

            // =========================================================
            // 8. REFRESH TOKEN
            // =========================================================
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.Property(r => r.Token)
                    .HasMaxLength(512);

                entity.HasIndex(r => r.Token)
                    .IsUnique();

                // Match AspNetUsers.Id length (varchar(255))
                entity.Property(r => r.UserId)
                    .HasMaxLength(255);

                entity.HasIndex(r => r.UserId);

                // IMPORTANT:
                // RefreshToken has a real User navigation.
                // Configure that exact navigation instead of
                // creating another anonymous relationship.
                entity.HasOne(r => r.User)
                    .WithMany()
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // =========================================================
            // 9. UTC DATETIME + MICROSECOND PRECISION
            // =========================================================
            var utcConverter =
                new ValueConverter<DateTime, DateTime>(
                    v => v.Kind == DateTimeKind.Utc
                        ? v
                        : DateTime.SpecifyKind(
                            v,
                            DateTimeKind.Utc),

                    v => DateTime.SpecifyKind(
                        v,
                        DateTimeKind.Utc));

            var nullableUtcConverter =
                new ValueConverter<DateTime?, DateTime?>(
                    v => !v.HasValue
                        ? v
                        : v.Value.Kind == DateTimeKind.Utc
                            ? v
                            : DateTime.SpecifyKind(
                                v.Value,
                                DateTimeKind.Utc),

                    v => !v.HasValue
                        ? v
                        : DateTime.SpecifyKind(
                            v.Value,
                            DateTimeKind.Utc));

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(utcConverter);
                        property.SetPrecision(6);
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(nullableUtcConverter);
                        property.SetPrecision(6);
                    }
                }
            }
        }
    }
}
