using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Social.Application.Features.Admin.AuditLogs.Queries;
using System;
using System.Threading.Tasks;

namespace Social.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/audit-logs")]
    [Authorize(Roles = "Admin")]
    public class AdminAuditLogsController : BaseController
    {
        private readonly ISender _sender;

        public AdminAuditLogsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? actionType = null,
            [FromQuery] string? adminId = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var result = await _sender.Send(new GetAuditLogsQuery(page, pageSize, actionType, adminId, fromDate, toDate));
            return ApiSuccess("Audit logs retrieved successfully", result);
        }
    }
}
