using Microsoft.AspNetCore.Mvc;
using Social.Core.Common;

namespace Social.API.Controllers
{
    public abstract class BaseController : ControllerBase
    {
        protected IActionResult ApiSuccess<T>(string message, T data)
        {
            return Ok(ApiResponse<T>.SuccessResponse(message, data));
        }

        protected IActionResult ApiError<T>(string message, object errors = null)
        {
            return BadRequest(ApiResponse<T>.ErrorResponse(message, errors));
        }

        protected IActionResult ApiNotFound<T>(string message = "Resource not found")
        {
            return NotFound(ApiResponse<T>.ErrorResponse(message));
        }

        protected IActionResult ApiUnauthorized<T>(string message = "Unauthorized access")
        {
            return Unauthorized(ApiResponse<T>.ErrorResponse(message));
        }

        protected IActionResult ApiServerError<T>(string message = "An error occurred", object errors = null)
        {
            return StatusCode(500, ApiResponse<T>.ErrorResponse(message, errors));
        }
    }
}