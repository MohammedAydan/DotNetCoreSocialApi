using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Social.Application.Features.Admin.Common;
using Social.Application.Features.Admin.Moderation.Commands;
using Social.Application.Features.Admin.Moderation.DTOs;
using Social.Application.Features.Admin.Moderation.Queries;
using System.Threading.Tasks;

namespace Social.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/moderation")]
    [Authorize(Roles = "Admin,Moderator")]
    public class AdminModerationController : BaseController
    {
        private readonly ISender _sender;

        public AdminModerationController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("feed")]
        public async Task<IActionResult> GetFeed([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _sender.Send(new GetModerationFeedQuery(page, pageSize));
            return ApiSuccess("Moderation feed retrieved successfully", result);
        }

        [HttpPost("posts/{postId}/hide")]
        public async Task<IActionResult> HidePost(string postId, [FromBody] AdminModerationActionRequest request)
        {
            var adminId = GetUserId();
            var adminEmail = GetUserEmail();
            var result = await _sender.Send(new HidePostCommand(adminId, adminEmail, postId, request?.Reason ?? string.Empty));
            return ApiSuccess("Post hidden successfully", result);
        }

        [HttpPost("posts/{postId}/restore")]
        public async Task<IActionResult> RestorePost(string postId, [FromBody] AdminModerationActionRequest request)
        {
            var adminId = GetUserId();
            var adminEmail = GetUserEmail();
            var result = await _sender.Send(new RestorePostCommand(adminId, adminEmail, postId, request?.Reason ?? string.Empty));
            return ApiSuccess("Post restored successfully", result);
        }

        [HttpPost("comments/{commentId}/hide")]
        public async Task<IActionResult> HideComment(string commentId, [FromBody] AdminModerationActionRequest request)
        {
            var adminId = GetUserId();
            var adminEmail = GetUserEmail();
            var result = await _sender.Send(new HideCommentCommand(adminId, adminEmail, commentId, request?.Reason ?? string.Empty));
            return ApiSuccess("Comment hidden successfully", result);
        }

        [HttpPost("comments/{commentId}/restore")]
        public async Task<IActionResult> RestoreComment(string commentId, [FromBody] AdminModerationActionRequest request)
        {
            var adminId = GetUserId();
            var adminEmail = GetUserEmail();
            var result = await _sender.Send(new RestoreCommentCommand(adminId, adminEmail, commentId, request?.Reason ?? string.Empty));
            return ApiSuccess("Comment restored successfully", result);
        }

        [HttpPost("posts/{postId}/visibility")]
        public async Task<IActionResult> UpdatePostVisibility(string postId, [FromBody] AdminUpdateVisibilityRequest request)
        {
            var adminId = GetUserId();
            var adminEmail = GetUserEmail();
            var result = await _sender.Send(new UpdatePostVisibilityCommand(adminId, adminEmail, postId, request?.Visibility ?? "public", request?.Reason ?? string.Empty));
            return ApiSuccess("Post visibility updated successfully", result);
        }

        [HttpDelete("posts/{postId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeletePostPermanently(string postId, [FromQuery] string? reason = null)
        {
            var adminId = GetUserId();
            var adminEmail = GetUserEmail();
            var result = await _sender.Send(new DeletePostPermanentlyCommand(adminId, adminEmail, postId, reason ?? "Permanently deleted by administrator"));
            return ApiSuccess("Post permanently deleted successfully", result);
        }
    }
}
