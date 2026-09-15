using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Social.Core.Entities;
using Social.Infrastructure.Data;

namespace Social.Infrastructure.Telemetry
{
    /// <summary>
    /// Drains the telemetry channel and batch-inserts every 5 seconds.
    /// No-op (drain-and-discard) in the Testing environment so the
    /// integration-test host never touches MySQL from a background thread.
    /// </summary>
    public sealed class RequestLogFlushWorker : BackgroundService
    {
        private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(5);
        private const int MaxBatchSize = 500;

        private readonly RequestLogChannel _channel;
        private readonly IServiceScopeFactory _scopes;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<RequestLogFlushWorker> _logger;

        public RequestLogFlushWorker(
            RequestLogChannel channel,
            IServiceScopeFactory scopes,
            IHostEnvironment environment,
            ILogger<RequestLogFlushWorker> logger)
        {
            _channel = channel;
            _scopes = scopes;
            _environment = environment;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(FlushInterval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await FlushOnceAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Request telemetry flush failed; will retry on next tick.");
                }
            }
        }

        private async Task FlushOnceAsync(CancellationToken cancellationToken)
        {
            var batch = new List<RequestLog>(MaxBatchSize);
            while (batch.Count < MaxBatchSize && _channel.Reader.TryRead(out var log))
                batch.Add(log);

            if (batch.Count == 0)
                return;

            if (_environment.IsEnvironment("Testing"))
                return; // discard: integration tests use mock doubles, not MySQL.

            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.RequestLogs.AddRangeAsync(batch, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
