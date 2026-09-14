using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Social.Application.Features.BlockUser.Commands;
using Social.Application.Features.BlockUser.DTOs;
using Social.Application.Features.BlockUser.Queries;
using Social.Application.Features.BlockUser.Requests;
using Social.Core.Common;

namespace Social.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class BlockUserController : BaseController
    {
        private readonly ISender _sender;

        public BlockUserController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        [Route("blocked-users")]
        public async Task<IActionResult> GetBlockedUsers(int page = 1, int limit = 20)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return ApiUnauthorized<object>("User ID is required.");
                }

                var blockedUsers = await _sender.Send(new GetBlockedUsersQuery(userId, page, limit));

                return ApiSuccess<IEnumerable<BlockUserDto>>(message: "Blocked users retrieved successfully.", data: blockedUsers);
            }
            catch (System.Exception ex)
            {
                return ApiError<object>("An error occurred while retrieving blocked users.", ex.Message);
            }
        }

        [HttpGet]
        [Route("is-blocked")]
        public async Task<IActionResult> IsUserBlocked(string blockedUserId)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return ApiUnauthorized<object>("User ID is required.");
                }

                var isBlocked = await _sender.Send(new IsUserBlockedQuery(userId, blockedUserId));

                return ApiSuccess<bool>(message: "Block status retrieved successfully.", data: isBlocked);
            }
            catch (System.Exception ex)
            {
                return ApiError<object>("An error occurred while checking block status.", ex.Message);
            }
        }

        [HttpPost]
        [Route("block")]
        public async Task<IActionResult> BlockUser([FromBody] BlockUserRequest blockUserRequest)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return ApiUnauthorized<object>("User ID is required.");
                }

                await _sender.Send(new BlockUserCommand(blockUserRequest, userId));

                return ApiSuccess<bool>(message: "User blocked successfully.", data: true);
            }
            catch (InvalidOperationException ex)
            {
                return ApiServerError<object>(ex.Message);
            }
            catch (System.Exception ex)
            {
                return ApiError<object>("An error occurred while blocking the user.", ex.Message);
            }
        }

        [HttpPost]
        [Route("unblock")]
        public async Task<IActionResult> UnblockUser([FromBody] BlockUserRequest blockUserRequest)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return ApiUnauthorized<object>("User ID is required.");
                }

                await _sender.Send(new UnblockUserCommand(blockUserRequest, userId));

                return ApiSuccess<bool>(message: "User unblocked successfully.", data: true);
            }
            catch (InvalidOperationException ex)
            {
                return ApiServerError<object>(ex.Message);
            }
            catch (System.Exception ex)
            {
                return ApiError<object>("An error occurred while unblocking the user.", ex.Message);
            }
        }
    }
}