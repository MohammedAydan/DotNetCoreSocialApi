using System.Threading.Channels;
using Social.Core.Entities;
using Social.Core.Interfaces;

namespace Social.Infrastructure.Telemetry
{
    /// <summary>
    /// Bounded in-memory channel decoupling the request hot path from MySQL
    /// batch inserts. Full channel drops (never blocks a user request).
    /// </summary>
    public sealed class RequestLogChannel : IRequestLogSink
    {
        private readonly Channel<RequestLog> _channel = Channel.CreateBounded<RequestLog>(
            new BoundedChannelOptions(10_000)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.DropWrite
            });

        public bool TryWrite(RequestLog log) => _channel.Writer.TryWrite(log);

        public ChannelReader<RequestLog> Reader => _channel.Reader;
    }
}
