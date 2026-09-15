using Social.Core.Entities;

namespace Social.Core.Interfaces
{
    /// <summary>
    /// Non-blocking sink for request telemetry. Implemented by a bounded
    /// <c>Channel&lt;RequestLog&gt;</c>; drops (never blocks) when full.
    /// </summary>
    public interface IRequestLogSink
    {
        bool TryWrite(RequestLog log);
    }
}
