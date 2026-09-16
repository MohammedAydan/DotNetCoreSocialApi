using MediatR;
using Microsoft.AspNetCore.Mvc;
using Social.Application.Features.Posts.Commands;
using Social.Application.Features.Posts.Queries;
using Social.Application.Features.Posts.DTOs;
using Social.Application.Features.Reports.Commands;
using Social.Application.Features.Reports.DTOs;
using Social.Application.Features.Reports.Queries;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Social.Core.Common;
using Social.Core.Interfaces;

namespace Social.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PostsController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ICacheService _cache;

        public PostsController(IMediator mediator, ICacheService cache)
        {
            _mediator = mediator;
            _cache = cache;
        }

        [HttpPost]
        public async Task<IActionResult> AddPost([FromBody] CreatePostRequest createPost)
        {
            if (createPost == null)
                return ApiError<object>("Post content is required.");

            try
            {
                var userId = GetUserId();
                var post = await _mediator.Send(new AddPostCommand(createPost, userId));
                
                return ApiSuccess("Post created successfully", post);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>($"An error occurred: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("share")]
        public async Task<IActionResult> SharePost([FromBody] SharePostRequest sharePost)
        {
            if (sharePost == null || string.IsNullOrWhiteSpace(sharePost.ParentPostId))
                return ApiError<object>("Post ID is required.");
            try
            {
                var userId = GetUserId();
                var post = await _mediator.Send(new SharePostCommand(sharePost, userId));
                return ApiSuccess("Post shared successfully", post);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>($"An error occurred: {ex.Message}");
            }
        }

        [HttpGet("my-posts")]
        public async Task<IActionResult> GetMyPosts([FromQuery] int Page = 1, [FromQuery] int Limit = 20)
        {
            try
            {
                var userId = GetUserId();
                var result = await _mediator.Send(new GetMyPostsQuery(userId, Page, Limit));
                return ApiSuccess("Posts retrieved successfully", result);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>($"An error occurred: {ex.Message}");
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetPostsByUserId([FromRoute] string userId,[FromQuery] int Page = 1, [FromQuery] int Limit = 20)
        {
            try
            {
                var myUserId = GetUserId();
                var result = await _mediator.Send(new GetPostsByUserIdQuery(userId, myUserId, Page, Limit));
                return ApiSuccess("Posts retrieved successfully", result);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>($"An error occurred: {ex.Message}");
            }
        }

        [HttpGet("feed")]
        public async Task<IActionResult> GetFeed([FromQuery] int Page = 1, [FromQuery] int Limit = 20)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrWhiteSpace(userId))
                    return ApiUnauthorized<object>("User ID is required.");
                var result = await _mediator.Send(new GetFeedPostsQuery(userId, Page, Limit));
                return ApiSuccess("Feed retrieved successfully", result);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>($"An error occurred: {ex.Message}");
            }
        }

        [HttpGet("{postId}")]
        public async Task<IActionResult> GetPostById(string postId)
        {
            if (string.IsNullOrWhiteSpace(postId))
                return ApiError<object>("Post ID is required.");

            try
            {
                var userId = GetUserId();
                var cacheKey = $"post:{postId}:user:{userId}";
                
                // Try to get from cache first
                var cachedPost = await _cache.GetAsync<PostDto>(cacheKey);
                if (cachedPost != null)
                {
                    return ApiSuccess("Post retrieved successfully (cached)", cachedPost);
                }
                
                // Get from database
                var post = await _mediator.Send(new GetPostByIdQuery(postId, userId));
                if (post == null)
                {
                    return ApiNotFound<object>("Post not found");
                }
                
                // Cache for 5 minutes
                await _cache.SetAsync(cacheKey, post, TimeSpan.FromMinutes(5));
                
                return ApiSuccess("Post retrieved successfully", post);
            }
            catch (Exception ex)
            {
                return ApiNotFound<object>(ex.Message);
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdatePost([FromBody] UpdatePostRequest updatePost)
        {
            if (updatePost == null || string.IsNullOrWhiteSpace(updatePost.Id))
                return ApiError<object>("Post data is required.");

            try
            {
                var userId = GetUserId();
                var post = await _mediator.Send(new UpdatePostCommand(updatePost, userId));
                
                // Invalidate single post cache
                await _cache.RemoveAsync($"post:{updatePost.Id}:user:{userId}");
                
                return ApiSuccess("Post updated successfully", post);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>($"An error occurred: {ex.Message}");
            }
        }

        [HttpDelete("{postId}")]
        public async Task<IActionResult> DeletePost(string postId)
        {
            if (string.IsNullOrWhiteSpace(postId))
                return ApiError<object>("Post ID is required.");

            try
            {
                var userId = GetUserId();
                var result = await _mediator.Send(new DeletePostCommand(postId, userId));
                
                if (result)
                {
                    // Invalidate single post cache
                    await _cache.RemoveAsync($"post:{postId}:user:{userId}");
                }
                
                return result ? ApiSuccess<object>("Post deleted successfully", null) : ApiNotFound<object>("Post not found or access denied.");
            }
            catch (Exception ex)
            {
                return ApiServerError<object>($"An error occurred: {ex.Message}");
            }
        }

        // Literal segment wins over "{postId}" in route precedence, so this
        // never clashes with GetPostById. Page/Limit stay capitalized to match
        // every other posts route (compass §3).
        [HttpGet("reports/mine")]
        public async Task<IActionResult> GetMyReports([FromQuery] int Page = 1, [FromQuery] int Limit = 20)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrWhiteSpace(userId))
                    return ApiUnauthorized<object>("User ID is required.");
                var result = await _mediator.Send(new GetMyReportsQuery(userId, Page, Limit));
                return ApiSuccess("Reports retrieved successfully", result);
            }
            catch (ArgumentException ex)
            {
                return ApiError<object>(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>($"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("{postId}/report")]
        public async Task<IActionResult> ReportPost(string postId, [FromBody] ReportPostRequest request)
        {
            if (string.IsNullOrWhiteSpace(postId))
                return ApiError<object>("Post ID is required.");
            if (request == null || string.IsNullOrWhiteSpace(request.Reason))
                return ApiError<object>("Reason is required.");

            try
            {
                var userId = GetUserId();
                if (string.IsNullOrWhiteSpace(userId))
                    return ApiUnauthorized<object>("User ID is required.");
                var result = await _mediator.Send(new ReportPostCommand(postId, userId, request.Reason, request.Details));
                return ApiSuccess("Post reported successfully", result);
            }
            catch (FluentValidation.ValidationException ex)
            {
                var errors = ex.Errors.Select(e => new { Field = e.PropertyName, Error = e.ErrorMessage }).ToList();
                return ApiError<object>("Validation failed.", errors);
            }
            catch (KeyNotFoundException ex)
            {
                return ApiNotFound<object>(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ApiError<object>(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return ApiError<object>(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>($"An error occurred: {ex.Message}");
            }
        }

        [HttpDelete("reports/{reportId}")]
        public async Task<IActionResult> CancelReport(string reportId)
        {
            if (string.IsNullOrWhiteSpace(reportId))
                return ApiError<object>("Report ID is required.");

            try
            {
                var userId = GetUserId();
                if (string.IsNullOrWhiteSpace(userId))
                    return ApiUnauthorized<object>("User ID is required.");
                var result = await _mediator.Send(new CancelReportCommand(reportId, userId));
                return result ? ApiSuccess<object>("Report cancelled successfully", null) : ApiNotFound<object>("Report not found.");
            }
            catch (KeyNotFoundException ex)
            {
                return ApiNotFound<object>(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return ApiUnauthorized<object>(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ApiError<object>(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>($"An error occurred: {ex.Message}");
            }
        }
    }
}
