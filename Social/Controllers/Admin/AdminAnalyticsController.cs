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
    }
}
