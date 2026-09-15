using MediatR;
using Social.Application.Features.Admin.Analytics.DTOs;
using Social.Core.Interfaces;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Social.Application.Features.Admin.Analytics.Queries
{
    public record GetSystemDiagnosticsQuery(string EnvironmentName) : IRequest<SystemDiagnosticsDto>;

    public class GetSystemDiagnosticsQueryHandler : IRequestHandler<GetSystemDiagnosticsQuery, SystemDiagnosticsDto>
    {
        private readonly ICacheService _cacheService;

        public GetSystemDiagnosticsQueryHandler(ICacheService cacheService)
        {
            _cacheService = cacheService;
        }

        public Task<SystemDiagnosticsDto> Handle(GetSystemDiagnosticsQuery request, CancellationToken cancellationToken)
        {
            using var process = Process.GetCurrentProcess();
            var memoryMb = Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 2);

            var isRedis = _cacheService.GetType().Name.Contains("Redis", StringComparison.OrdinalIgnoreCase);
            var isConnected = isRedis;
            var usingMemoryFallback = !isRedis;

            return Task.FromResult(new SystemDiagnosticsDto
            {
                IsConnected = isConnected,
                UsingMemoryFallback = usingMemoryFallback,
                TrackedIpCount = 1,
                ThrottledRequestsCount = 0,
                MemoryWorkingSetMb = memoryMb,
                Environment = request.EnvironmentName
            });
        }
    }
}
