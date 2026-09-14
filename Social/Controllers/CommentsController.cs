using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Social.Application.Features.Comments.Commands;
using Social.Application.Features.Comments.DTOs;
using Social.Application.Features.Comments.Queries;
using Social.Core.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Social.Core.Interfaces;

namespace Social.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CommentsController : BaseController
    {
        private readonly ISender _sender;
        private readonly ICacheService _cache;

        public CommentsController(ISender sender, ICacheService cache)
        {
            _sender = sender;
            _cache = cache;
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
                
                // Invalidate post cache (comment count changed)
                await _cache.RemoveAsync($"post:{commentRequest.PostId}:user:{userId}");
                
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
                
                // Invalidate parent comment cache
                await _cache.RemoveAsync($"comment:{commentRequest.ParentId}");
                
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
                
                // Invalidate comment cache
                await _cache.RemoveAsync($"comment:{commentId}");
                
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
                
                // Invalidate comment cache
                await _cache.RemoveAsync($"comment:{commentId}");
                
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
        [Route("replies/{parentId}")]
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
                
                var cacheKey = $"comment:{commentId}";
                
                // Try cache first
                var cached = await _cache.GetAsync<object>(cacheKey);
                if (cached != null)
                {
                    return ApiSuccess("Comment retrieved successfully (cached)", cached);
                }
                
                var result = await _sender.Send(new GetCommentByIdQuery(commentId));
                
                // Cache for 3 minutes (single comment)
                await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(3));
                
                return ApiSuccess("Comment retrieved successfully", result);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>($"An error occurred: {ex.Message}");
            }
        }
    }
}
