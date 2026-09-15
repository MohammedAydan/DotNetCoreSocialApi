using Microsoft.EntityFrameworkCore;
using Social.Core.Interfaces;
using Social.Infrastructure.Data;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Social.Infrastructure.Diagnostics
{
    public class SystemMetricsService : ISystemMetricsService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICacheService _cacheService;

        public SystemMetricsService(ApplicationDbContext context, ICacheService cacheService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        }

        public async Task<(int TotalUsers, int ActiveUsers24h, int TotalPosts, int TotalComments, int TotalLikes)> GetOverviewMetricsAsync(CancellationToken cancellationToken = default)
        {
            var totalUsers = await _context.Users.CountAsync(cancellationToken);
            var since24h = DateTime.UtcNow.AddHours(-24);
            var activeUsers24h = await _context.Users.CountAsync(u => u.UpdatedAt >= since24h, cancellationToken);
            var totalPosts = await _context.Posts.CountAsync(p => !p.IsDeleted, cancellationToken);
            var totalComments = await _context.Comments.CountAsync(c => !c.IsDeleted, cancellationToken);
            var totalLikes = await _context.Likes.CountAsync(cancellationToken);

            return (totalUsers, activeUsers24h, totalPosts, totalComments, totalLikes);
        }

        public Task<(bool IsConnected, bool UsingMemoryFallback, long TrackedIpCount, long ThrottledRequestsCount, double MemoryWorkingSetMb, string Environment)> GetDiagnosticsAsync(string environmentName, CancellationToken cancellationToken = default)
        {
            using var process = Process.GetCurrentProcess();
            var memoryMb = Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 2);

            var isRedis = _cacheService.GetType().Name.Contains("Redis", StringComparison.OrdinalIgnoreCase);
            var isConnected = isRedis;
            var usingMemoryFallback = !isRedis;

            return Task.FromResult((
                IsConnected: isConnected,
                UsingMemoryFallback: usingMemoryFallback,
                TrackedIpCount: 1L,
                ThrottledRequestsCount: 0L,
                MemoryWorkingSetMb: memoryMb,
                Environment: environmentName
            ));
        }
    }
}
