using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Social.Application.Features.Notifications.Commands;
using Social.Application.Features.Notifications.DTOs;
using Social.Application.Features.Notifications.Queries;
using System.Security.Claims;
using Social.Core.Interfaces;

namespace Social.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ICacheService _cache;

        public NotificationsController(IMediator mediator, ICacheService cache)
        {
            _mediator = mediator;
            _cache = cache;
        }

        [HttpPost]
        public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationDto createNotification)
        {
            try
            {
                if (string.IsNullOrEmpty(GetUserId()))
                    return ApiUnauthorized<object>("User ID is required.");

                var result = await _mediator.Send(new CreateNotificationCommand(createNotification));
                
                // Invalidate single notification cache only
                if (result != null && result.Id != null)
                {
                    await _cache.RemoveAsync($"notification:{result.Id}");
                }
                
                return ApiSuccess("Notification created successfully.", result);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>("Failed to create notification.", ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var cacheKey = $"notification:{id}";
                
                // Try cache first
                var cached = await _cache.GetAsync<object>(cacheKey);
                if (cached != null)
                {
                    return ApiSuccess("Notification retrieved successfully (cached).", cached);
                }
                
                var result = await _mediator.Send(new GetNotificationByIdQuery(id));
                
                // Cache for 2 minutes (notifications should be fresh)
                await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(2));
                
                return ApiSuccess("Notification retrieved successfully.", result);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>("Failed to retrieve notification.", ex.Message);
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUserId(string userId, int page = 1, int limit = 20)
        {
            try
            {
                if (!IsAuthorizedUser(userId))
                    return ApiUnauthorized<object>("Unauthorized access.");

                var result = await _mediator.Send(new GetNotificationsByUserIdQuery(userId, page, limit));
                
                return ApiSuccess("Notifications retrieved successfully.", result);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>("Failed to retrieve notifications.", ex.Message);
            }
        }

        [HttpGet("user/{userId}/unread")]
        public async Task<IActionResult> GetUnreadByUserId(string userId, int page = 1, int limit = 20)
        {
            try
            {
                if (!IsAuthorizedUser(userId))
                    return ApiUnauthorized<object>("Unauthorized access.");

                var result = await _mediator.Send(new GetUnreadNotificationsByUserIdQuery(userId, page, limit));
                
                return ApiSuccess("Unread notifications retrieved successfully.", result);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>("Failed to retrieve unread notifications.", ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateNotificationDto dto)
        {
            try
            {
                if (!IsAuthorizedUser(dto.UserId))
                    return ApiUnauthorized<object>("Unauthorized access.");

                var result = await _mediator.Send(new UpdateNotificationCommand(dto));
                
                // Invalidate single notification cache
                await _cache.RemoveAsync($"notification:{id}");
                
                return ApiSuccess("Notification updated successfully.", result);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>("Failed to update notification.", ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                if (!IsAuthorizedUser(id))
                    return ApiUnauthorized<object>("Unauthorized access.");

                await _mediator.Send(new DeleteNotificationCommand(id));
                
                // Invalidate single notification cache
                await _cache.RemoveAsync($"notification:{id}");
                
                return ApiSuccess<object>("Notification deleted successfully.", null);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>("Failed to delete notification.", ex.Message);
            }
        }

        [HttpPost("{id}/mark-read")]
        public async Task<IActionResult> MarkAsRead(string id)
        {
            try
            {
                if (!IsAuthorizedUser(id))
                    return ApiUnauthorized<object>("Unauthorized access.");

                await _mediator.Send(new MarkNotificationAsReadCommand(id));
                
                // Invalidate single notification cache
                await _cache.RemoveAsync($"notification:{id}");
                
                return ApiSuccess<object>("Notification marked as read.", null);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>("Failed to mark notification as read.", ex.Message);
            }
        }

        [HttpPost("user/{userId}/mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead(string userId)
        {
            try
            {
                if (!IsAuthorizedUser(userId))
                    return ApiUnauthorized<object>("Unauthorized access.");

                await _mediator.Send(new MarkAllNotificationsAsReadCommand(userId));
                
                return ApiSuccess<object>("All notifications marked as read.", null);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>("Failed to mark all notifications as read.", ex.Message);
            }
        }

        [HttpDelete("user/{userId}/all")]
        public async Task<IActionResult> DeleteAllForUser(string userId)
        {
            try
            {
                if (!IsAuthorizedUser(userId))
                    return ApiUnauthorized<object>("Unauthorized access.");

                await _mediator.Send(new DeleteAllNotificationsForUserCommand(userId));
                
                return ApiSuccess<object>("All notifications deleted successfully.", null);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>("Failed to delete all notifications for user.", ex.Message);
            }
        }

        private bool IsAuthorizedUser(string userId)
        {
            var _userId = GetUserId();
            return !string.IsNullOrEmpty(_userId) && userId == _userId;
        }
    }
}
