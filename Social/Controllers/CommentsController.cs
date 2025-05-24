using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Social.Application.Features.Comments.Commands;
using Social.Application.Features.Comments.DTOs;
using Social.Application.Features.Comments.Queries;
using Social.Core.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Social.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CommentsController : BaseController
    {
        private readonly ISender _sender;

        public CommentsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        public async Task<IActionResult> CreateComment([FromBody] CreateCommentRequest commentRequest)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return ApiUnauthorized<object>("User ID is required.");
                }

                if (commentRequest == null || string.IsNullOrWhiteSpace(commentRequest.PostId) || string.IsNullOrWhiteSpace(commentRequest.Content))
                {
                    return ApiError<object>("PostId and Content are required.");
                }
                var result = await _sender.Send(new AddCommentCommand(commentRequest, userId));
                return ApiSuccess("Comment created successfully", result);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>($"An error occurred: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("reply")]
        public async Task<IActionResult> CreateReplyComment([FromBody] CreateReplyCommentRequest commentRequest)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return ApiUnauthorized<object>("User ID is required.");
                }

                if (commentRequest == null || string.IsNullOrWhiteSpace(commentRequest.PostId) || string.IsNullOrWhiteSpace(commentRequest.Content))
                {
                    return ApiError<object>("PostId and Content are required.");
                }

                if (string.IsNullOrWhiteSpace(commentRequest.ParentId))
                {
                    return ApiError<object>("ParentId is required.");
                }

                var result = await _sender.Send(new AddReplyCommentCommand(commentRequest, userId));
                return ApiSuccess("Reply comment created successfully", result);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>($"An error occurred: {ex.Message}");
            }
        }

        [HttpPut]
        [Route("{commentId}")]
        public async Task<IActionResult> UpdateComment([FromRoute] string commentId, [FromBody] UpdateCommentRequest commentRequest)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return ApiUnauthorized<object>("User ID is required.");
                }
                if (string.IsNullOrWhiteSpace(commentId))
                {
                    return ApiError<object>("CommentId is required.");
                }
                if (commentRequest == null || string.IsNullOrWhiteSpace(commentRequest.Content))
                {
                    return ApiError<object>("Content is required.");
                }
                if (commentRequest.Id != commentId)
                {
                    return ApiError<object>("CommentId mismatch.");
                }

                var result = await _sender.Send(new UpdateCommentCommand(commentRequest, userId));
                return ApiSuccess("Comment updated successfully", result);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>($"An error occurred: {ex.Message}");
            }
        }

        [HttpDelete]
        [Route("{commentId}")]
        public async Task<IActionResult> DeleteComment([FromRoute] string commentId)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return ApiUnauthorized<object>("User ID is required.");
                }
                if (string.IsNullOrWhiteSpace(commentId))
                {
                    return ApiError<object>("CommentId is required.");
                }
                var result = await _sender.Send(new DeleteCommentCommand(commentId, userId));
                return ApiSuccess("Comment deleted successfully", result);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>($"An error occurred: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("post/{postId}")]
        public async Task<IActionResult> GetCommentsByPostId([FromRoute] string postId, [FromQuery] int page = 1, [FromQuery] int limit = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(postId))
                {
                    return ApiError<object>("PostId is required.");
                }
                var result = await _sender.Send(new GetCommentsByPostIdQuery(postId, page, limit));
                return ApiSuccess("Comments retrieved successfully", result);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>($"An error occurred: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("reply/{parentId}")]
        public async Task<IActionResult> GetReplyCommentsByParentCommentId([FromRoute] string parentId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(parentId))
                {
                    return ApiError<object>("ParentId is required.");
                }
                var result = await _sender.Send(new GetReplyCommentsByParentCommentIdQuery(parentId));
                return ApiSuccess("Reply comments retrieved successfully", result);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>($"An error occurred: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("{commentId}")]
        public async Task<IActionResult> GetCommentById([FromRoute] string commentId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(commentId))
                {
                    return ApiError<object>("CommentId is required.");
                }
                var result = await _sender.Send(new GetCommentByIdQuery(commentId));
                return ApiSuccess("Comment retrieved successfully", result);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>($"An error occurred: {ex.Message}");
            }
        }

        private string GetUserId()
        {
            return User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }
    }
}
