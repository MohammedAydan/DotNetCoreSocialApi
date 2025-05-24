using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Social.Application.Features.Notifications.Commands;
using Social.Application.Features.Notifications.DTOs;
using Social.Application.Features.Notifications.Queries;
using System.Security.Claims;

namespace Social.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : BaseController
    {
        private readonly IMediator _mediator;

        public NotificationsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationDto createNotification)
        {
            try
            {
                if (string.IsNullOrEmpty(GetUserId()))
                    return ApiUnauthorized<object>("User ID is required.");

                var result = await _mediator.Send(new CreateNotificationCommand(createNotification));
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
                if (!IsAuthorizedUser(id))
                    return ApiUnauthorized<object>("Unauthorized access.");

                var result = await _mediator.Send(new GetNotificationByIdQuery(id));
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

        [HttpGet("user/{userId}/paged")]
        public async Task<IActionResult> GetPagedByUserId(string userId, int page = 1, int limit = 20)
        {
            try
            {
                if (!IsAuthorizedUser(userId))
                    return ApiUnauthorized<object>("Unauthorized access.");

                var result = await _mediator.Send(new GetPagedNotificationsByUserIdQuery(userId, page, limit));
                return ApiSuccess("Paged notifications retrieved successfully.", new
                {
                    data = result.Item1,
                    total = result.Item2
                });
            }
            catch (Exception ex)
            {
                return ApiServerError<object>("Failed to retrieve paged notifications.", ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateNotificationDto dto)
        {
            try
            {
                if (!IsAuthorizedUser(dto.UserId))
                    return ApiUnauthorized<object>("Unauthorized access.");

                if (id != dto.Id)
                    return ApiError<object>("ID mismatch.");

                var result = await _mediator.Send(new UpdateNotificationCommand(dto));
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
                return ApiSuccess<object>("All notifications deleted for user.", null);
            }
            catch (Exception ex)
            {
                return ApiServerError<object>("Failed to delete all notifications for user.", ex.Message);
            }
        }

        private string GetUserId()
        {
            return User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }

        private bool IsAuthorizedUser(string userId)
        {
            var _userId = GetUserId();
            return !string.IsNullOrEmpty(_userId) && userId == _userId;
        }
    }
}
