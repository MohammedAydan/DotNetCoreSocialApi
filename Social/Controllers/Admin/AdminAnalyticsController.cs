using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Social.Application.Features.Admin.Analytics.Queries;
using System.Threading.Tasks;

namespace Social.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/analytics")]
    [Authorize(Roles = "Admin,Moderator")]
    public class AdminAnalyticsController : BaseController
    {
        private readonly ISender _sender;
        private readonly IWebHostEnvironment _environment;

        public AdminAnalyticsController(ISender sender, IWebHostEnvironment environment)
        {
            _sender = sender;
            _environment = environment;
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview()
        {
            var result = await _sender.Send(new GetPlatformOverviewQuery());
            return ApiSuccess("Overview metrics retrieved successfully", result);
        }

        [HttpGet("diagnostics")]
        public async Task<IActionResult> GetDiagnostics()
        {
            var result = await _sender.Send(new GetSystemDiagnosticsQuery(_environment.EnvironmentName));
            return ApiSuccess("Diagnostics retrieved successfully", result);
        }

        [HttpGet("kpi-summary")]
        public async Task<IActionResult> GetKpiSummary()
        {
            var result = await _sender.Send(new GetKpiSummaryQuery());
            return ApiSuccess("KPI summary retrieved successfully", result);
        }

        [HttpGet("user-growth")]
        public async Task<IActionResult> GetUserGrowth([FromQuery] string range = "30d")
        {
            var result = await _sender.Send(new GetUserGrowthQuery(range));
            return ApiSuccess("User growth retrieved successfully", result);
        }

        [HttpGet("content-velocity")]
        public async Task<IActionResult> GetContentVelocity([FromQuery] int days = 30)
        {
            var result = await _sender.Send(new GetContentVelocityQuery(days));
            return ApiSuccess("Content velocity retrieved successfully", result);
        }

        [HttpGet("api-health")]
        public async Task<IActionResult> GetApiHealth()
        {
            var result = await _sender.Send(new GetApiHealthQuery());
            return ApiSuccess("API health retrieved successfully", result);
        }

        [HttpGet("safety-metrics")]
        public async Task<IActionResult> GetSafetyMetrics([FromQuery] int days = 30)
        {
            var result = await _sender.Send(new GetSafetyMetricsQuery(days));
            return ApiSuccess("Safety metrics retrieved successfully", result);
        }

        [HttpGet("request-stream")]
        public async Task<IActionResult> GetRequestStream([FromQuery] int take = 50)
        {
            var result = await _sender.Send(new GetRequestStreamQuery(take));
            return ApiSuccess("Request stream retrieved successfully", result);
        }
    }
}
