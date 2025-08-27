using Microsoft.AspNetCore.Mvc;
using Social.Application.Features.Follow.Commands;
using Social.Application.Features.Follow.Queries;
using Social.Application.Features.Follow.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Social.Core.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Social.Application.Features.Followers.DTOs;

namespace Social.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class FollowController : BaseController
    {
        private readonly ISender _sender;

        public FollowController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost]
        [Route("follow")]
        public async Task<IActionResult> Follow([FromBody] FollowRequest followRequest)
        {
            try
            {
                if (followRequest == null || string.IsNullOrWhiteSpace(followRequest.FollowerId) || string.IsNullOrWhiteSpace(followRequest.TargetUserId))
                {
                    return ApiError<object>("FollowerId and FolloweeId are required.");
                }
                var result = await _sender.Send(new FollowUserCommand(followRequest));
                return ApiSuccess<FollowerDto>("Follow request sent successfully", result);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>($"An error occurred: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("unfollow")]
        public async Task<IActionResult> Unfollow([FromBody] FollowRequest unfollowRequest)
        {
            try
            {
                if (unfollowRequest == null || string.IsNullOrWhiteSpace(unfollowRequest.FollowerId) || string.IsNullOrWhiteSpace(unfollowRequest.TargetUserId))
                {
                    return ApiError<object>("FollowerId and FolloweeId are required.");
                }
                var result = await _sender.Send(new UnfollowUserCommand(unfollowRequest));
                return ApiSuccess<bool>("Unfollowed successfully", result);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>($"An error occurred: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("accept-follow-request")]
        public async Task<IActionResult> AcceptFollowRequest([FromBody] FollowRequest acceptRequest)
        {
            try
            {
                if (acceptRequest == null || string.IsNullOrWhiteSpace(acceptRequest.FollowerId) || string.IsNullOrWhiteSpace(acceptRequest.TargetUserId))
                {
                    return ApiError<object>("FollowerId and FolloweeId are required.");
                }
                var result = await _sender.Send(new AcceptFollowCommand(acceptRequest));
                return ApiSuccess<bool>("Follow request accepted", result);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>($"An error occurred: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("reject-follow-request")]
        public async Task<IActionResult> RejectFollowRequest([FromBody] FollowRequest rejectRequest)
        {
            try
            {
                if (rejectRequest == null || string.IsNullOrWhiteSpace(rejectRequest.FollowerId) || string.IsNullOrWhiteSpace(rejectRequest.TargetUserId))
                {
                    return ApiError<object>("FollowerId and FolloweeId are required.");
                }
                var result = await _sender.Send(new RejectFollowCommand(rejectRequest));
                return ApiSuccess<bool>("Follow request rejected", result);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>($"An error occurred: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("followers")]
        public async Task<IActionResult> GetFollowers([FromQuery] int page = 1, [FromQuery] int limit = 20)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                {
                    return ApiUnauthorized<object>("User ID is required.");
                }

                var result = await _sender.Send(new GetFollowersQuery(userId, page, limit));
                return ApiSuccess<IEnumerable<FollowerDto>>("Followers retrieved successfully", result);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>($"An error occurred: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("following")]
        public async Task<IActionResult> GetFollowing([FromQuery] int page = 1, [FromQuery] int limit = 20)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                {
                    return ApiUnauthorized<object>("User ID is required.");
                }

                var result = await _sender.Send(new GetFollowingQuery(userId, page, limit));
                return ApiSuccess<IEnumerable<FollowerDto>>("Following list retrieved successfully", result);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>($"An error occurred: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("pending-follow-requests")]
        public async Task<IActionResult> GetPendingFollowRequests([FromQuery] int page = 1, [FromQuery] int limit = 20)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                {
                    return ApiUnauthorized<object>("User ID is required.");
                }

                var result = await _sender.Send(new GetPendingFollowRequestQuery(userId, page, limit));
                return ApiSuccess<IEnumerable<FollowerDto>>("Pending follow requests retrieved successfully", result);
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