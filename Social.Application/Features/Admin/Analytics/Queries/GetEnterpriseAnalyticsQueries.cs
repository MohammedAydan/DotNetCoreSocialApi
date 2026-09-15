using MediatR;
using Social.Core.Analytics;
using Social.Core.Interfaces;

namespace Social.Application.Features.Admin.Analytics.Queries
{
    public record GetKpiSummaryQuery : IRequest<KpiSummaryDto>;

    public class GetKpiSummaryQueryHandler : IRequestHandler<GetKpiSummaryQuery, KpiSummaryDto>
    {
        private readonly IAnalyticsService _analytics;
        public GetKpiSummaryQueryHandler(IAnalyticsService analytics) => _analytics = analytics;
        public Task<KpiSummaryDto> Handle(GetKpiSummaryQuery request, CancellationToken cancellationToken) =>
            _analytics.GetKpiSummaryAsync(cancellationToken);
    }

    public record GetUserGrowthQuery(string Range = "30d") : IRequest<UserGrowthDto>;

    public class GetUserGrowthQueryHandler : IRequestHandler<GetUserGrowthQuery, UserGrowthDto>
    {
        private readonly IAnalyticsService _analytics;
        public GetUserGrowthQueryHandler(IAnalyticsService analytics) => _analytics = analytics;
        public Task<UserGrowthDto> Handle(GetUserGrowthQuery request, CancellationToken cancellationToken) =>
            _analytics.GetUserGrowthAsync(request.Range, cancellationToken);
    }

    public record GetContentVelocityQuery(int Days = 30) : IRequest<ContentVelocityDto>;

    public class GetContentVelocityQueryHandler : IRequestHandler<GetContentVelocityQuery, ContentVelocityDto>
    {
        private readonly IAnalyticsService _analytics;
        public GetContentVelocityQueryHandler(IAnalyticsService analytics) => _analytics = analytics;
        public Task<ContentVelocityDto> Handle(GetContentVelocityQuery request, CancellationToken cancellationToken) =>
            _analytics.GetContentVelocityAsync(request.Days, cancellationToken);
    }

    public record GetApiHealthQuery : IRequest<ApiHealthDto>;

    public class GetApiHealthQueryHandler : IRequestHandler<GetApiHealthQuery, ApiHealthDto>
    {
        private readonly IAnalyticsService _analytics;
        public GetApiHealthQueryHandler(IAnalyticsService analytics) => _analytics = analytics;
        public Task<ApiHealthDto> Handle(GetApiHealthQuery request, CancellationToken cancellationToken) =>
            _analytics.GetApiHealthAsync(cancellationToken);
    }

    public record GetSafetyMetricsQuery(int Days = 30) : IRequest<SafetyMetricsDto>;

    public class GetSafetyMetricsQueryHandler : IRequestHandler<GetSafetyMetricsQuery, SafetyMetricsDto>
    {
        private readonly IAnalyticsService _analytics;
        public GetSafetyMetricsQueryHandler(IAnalyticsService analytics) => _analytics = analytics;
        public Task<SafetyMetricsDto> Handle(GetSafetyMetricsQuery request, CancellationToken cancellationToken) =>
            _analytics.GetSafetyMetricsAsync(request.Days, cancellationToken);
    }

    public record GetRequestStreamQuery(int Take = 50) : IRequest<List<RequestStreamItem>>;

    public class GetRequestStreamQueryHandler : IRequestHandler<GetRequestStreamQuery, List<RequestStreamItem>>
    {
        private readonly IAnalyticsService _analytics;
        public GetRequestStreamQueryHandler(IAnalyticsService analytics) => _analytics = analytics;
        public Task<List<RequestStreamItem>> Handle(GetRequestStreamQuery request, CancellationToken cancellationToken) =>
            _analytics.GetRequestStreamAsync(request.Take, cancellationToken);
    }
}
