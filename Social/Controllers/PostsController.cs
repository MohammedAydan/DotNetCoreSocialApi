using MediatR;
using Microsoft.AspNetCore.Mvc;
using Social.Application.Features.Posts.Commands;
using Social.Application.Features.Posts.Queries;
using Social.Application.Features.Posts.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Social.Core.Common;
using Social.API.Controllers;

namespace Social.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PostsController : BaseController
    {
        private readonly IMediator _mediator;

        public PostsController(IMediator mediator)
        {
            _mediator = mediator;
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
                var post = await _mediator.Send(new GetPostByIdQuery(postId, userId));
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
                return result ? ApiSuccess<object>("Post deleted successfully", null) : ApiNotFound<object>("Post not found or access denied.");
            }
            catch (Exception ex)
            {
                return ApiServerError<object>($"An error occurred: {ex.Message}");
            }
        }

        private string GetUserId()
        {
            return User?.Claims.FirstOrDefault(c =>c.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }
    }
}
