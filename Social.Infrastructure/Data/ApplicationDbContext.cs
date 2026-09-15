using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Social.Core.Entities;
using System;

namespace Social.Infrastructure.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<User>(options)
    {
        public DbSet<Post> Posts { get; set; }
        public DbSet<Follower> Followers { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Media> Media { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<BlockUser> BlockUsers { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==========================================
            // 1. Post Configuration & Indexes
            // ==========================================
            modelBuilder.Entity<Post>(entity =>
            {
                // الفهرس الأساسي للـ Feed: الترتيب التنازلي الحتمي
                entity.HasIndex(p => new { p.CreatedAt, p.Id })
                      .IsDescending(true, true);

                // فهرس لجلب بوستات بروفايل مستخدم معين بسرعة
                entity.HasIndex(p => new { p.UserId, p.CreatedAt })
                      .IsDescending(false, true);

                // فهرس للفلترة حسب نوع الرؤية (Public / Private)
                entity.HasIndex(p => new { p.Visibility, p.CreatedAt })
                      .IsDescending(false, true);

                // ضبط أطوال النصوص لحماية فهارس MySQL من خطأ (Key size exceeds limit)
                entity.Property(p => p.Visibility).HasMaxLength(20);

                entity.HasOne(p => p.User)
                      .WithMany()
                      .HasForeignKey(p => p.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.ParentPost)
                      .WithMany()
                      .HasForeignKey(p => p.ParentPostId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ==========================================
            // 2. Follower Configuration & Indexes
            // ==========================================
            modelBuilder.Entity<Follower>(entity =>
            {
                entity.HasKey(f => f.Id);

                // منع تكرار المتابعة + تسريع فحص العلاقة المباشرة
                entity.HasIndex(f => new { f.FollowerId, f.FollowingId })
                      .IsUnique();

                // فهرس مركب مخصص للـ Feed query (_context.Followers.Any)
                entity.HasIndex(f => new { f.FollowerId, f.Accepted, f.FollowingId });

                // فهرس عكسي لتسريع جلب قائمة المتابعين (Followers List) والعدّ
                entity.HasIndex(f => new { f.FollowingId, f.Accepted });

                entity.HasOne(f => f.FollowerUser)
                      .WithMany(u => u.Following)
                      .HasForeignKey(f => f.FollowerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.FollowingUser)
                      .WithMany(u => u.Followers)
                      .HasForeignKey(f => f.FollowingId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ==========================================
            // 3. Like Configuration & Indexes
            // ==========================================
            modelBuilder.Entity<Like>(entity =>
            {
                // منع عمل أكثر من Like على نفس البوست + تسريع فحص الإعجابات في الـ Feed
                entity.HasIndex(l => new { l.UserId, l.PostId })
                      .IsUnique();

                // تسريع حساب عدد الإعجابات لكل بوست
                entity.HasIndex(l => l.PostId);

                entity.HasOne<Post>()
                      .WithMany(p => p.Likes)
                      .HasForeignKey(l => l.PostId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ==========================================
            // 4. Comment Configuration & Indexes
            // ==========================================
            modelBuilder.Entity<Comment>(entity =>
            {
                // تسريع جلب تعليقات البوست مرتبة زمنياً
                entity.HasIndex(c => new { c.PostId, c.CreatedAt })
                      .IsDescending(false, true);

                entity.HasIndex(c => c.UserId);

                entity.HasOne<Post>()
                      .WithMany(p => p.Comments)
                      .HasForeignKey(c => c.PostId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ==========================================
            // 5. Media Configuration
            // ==========================================
            modelBuilder.Entity<Media>(entity =>
            {
                // تسريع جلب وسائط البوستات عند استخدام SplitQuery أو Include
                entity.HasIndex(m => m.PostId);

                entity.HasOne<Post>()
                      .WithMany(p => p.Media)
                      .HasForeignKey(m => m.PostId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ==========================================
            // 6. BlockUser Configuration
            // ==========================================
            modelBuilder.Entity<BlockUser>(entity =>
            {
                // 1. مفتاح مركب أساسي (Primary Key) - يضمن الفرادة تلقائياً ويوفر مساحة
                entity.HasKey(b => new { b.UserId, b.BlockedUserId });

                // 2. فهرس عكسي ضروري جداً لفحص: "هل أنا محظور من الطرف الآخر؟"
                entity.HasIndex(b => new { b.BlockedUserId, b.UserId });

                // 3. منع أخطاء تضارب الحذف المتتالي (Multiple Cascade Paths)
                entity.HasOne(b => b.User)
                      .WithMany()
                      .HasForeignKey(b => b.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(b => b.BlockedUser)
                      .WithMany()
                      .HasForeignKey(b => b.BlockedUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ==========================================
            // 7. Notification Configuration
            // ==========================================
            modelBuilder.Entity<Notification>(entity =>
            {
                // تسريع جلب الإشعارات غير المقروءة للمستخدم
                entity.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt })
                      .IsDescending(false, false, true);
            });

            // ==========================================
            // 8. RefreshToken Configuration
            // ==========================================
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.Property(r => r.Token).HasMaxLength(512);
                entity.HasIndex(r => r.Token).IsUnique();

                entity.Property(r => r.UserId).HasMaxLength(450);
                entity.HasIndex(r => r.UserId);
            });

            // ==========================================
            // 9. UTC DateTime & Precision Setup
            // ==========================================
            var utcConverter = new ValueConverter<DateTime, DateTime>(
                v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            var nullableUtcConverter = new ValueConverter<DateTime?, DateTime?>(
                v => !v.HasValue ? v : (v.Value.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)),
                v => !v.HasValue ? v : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc));

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(utcConverter);
                        // ضروري جداً لـ MySQL لمنع تساوي الأوقات في الترتيب (Microsecond Precision)
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