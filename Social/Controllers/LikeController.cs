using MediatR;
using Microsoft.AspNetCore.Mvc;
using Social.Application.Features.Like.Commands;
using Social.Application.Features.Like.Queries;
using Social.Application.Features.Like.DTOs;
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
    public class LikeController : BaseController
    {
        private readonly ISender _sender;
        private readonly ICacheService _cache;

        public LikeController(ISender sender, ICacheService cache)
        {
            _sender = sender;
            _cache = cache;
        }

        [HttpPost]
        public async Task<IActionResult> AddOrRemoveLike([FromBody] LikeRequest likeRequest)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return ApiUnauthorized<object>("User ID is required.");
                }
                if (likeRequest == null || string.IsNullOrWhiteSpace(likeRequest.PostId))
                {
                    return ApiError<object>("PostId is required.");
                }
                var result = await _sender.Send(new AddOrRemoveLikeCommand(likeRequest, userId));
                
                // Invalidate post cache (like count changed)
                await _cache.RemoveAsync($"post:{likeRequest.PostId}:user:{userId}");
                
                return ApiSuccess<bool>("Like operation completed successfully", result);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>($"An error occurred: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("{postId}")]
        public async Task<IActionResult> GetLikesByPostId(string postId, int page = 1, int limit = 20)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return ApiUnauthorized<object>("User ID is required.");
                }

                if (string.IsNullOrWhiteSpace(postId))
                {
                    return ApiError<object>("PostId is required.");
                }
                
                var result = await _sender.Send(new GetLikesByPostIdQuery(postId, page, limit));
                
                return ApiSuccess<IEnumerable<LikeDto>>("Likes retrieved successfully", result);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>($"An error occurred: {ex.Message}");
            }
        }
    }
}
