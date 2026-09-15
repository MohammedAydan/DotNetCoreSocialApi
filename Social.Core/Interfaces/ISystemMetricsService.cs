using System;
using System.Threading;
using System.Threading.Tasks;

namespace Social.Core.Interfaces
{
    public interface ISystemMetricsService
    {
        Task<(int TotalUsers, int ActiveUsers24h, int TotalPosts, int TotalComments, int TotalLikes)> GetOverviewMetricsAsync(CancellationToken cancellationToken = default);
        Task<(bool IsConnected, bool UsingMemoryFallback, long TrackedIpCount, long ThrottledRequestsCount, double MemoryWorkingSetMb, string Environment)> GetDiagnosticsAsync(string environmentName, CancellationToken cancellationToken = default);
    }
}
