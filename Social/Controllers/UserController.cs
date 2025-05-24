using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Social.Application.Features.Users.Commands;
using Social.Application.Features.Users.Commends;
using Social.Application.Features.Users.DTOs;
using Social.Application.Features.Users.Queries;
using Social.Core.Common;
using Social.Core.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Social.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : BaseController
    {
        private readonly ISender _sender;

        public UserController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] CreateUserRequest createUserRequest)
        {
            try
            {
                if (createUserRequest == null)
                {
                    return ApiError<AuthResponse>("Request body cannot be null.", new List<string> { "Request body cannot be null." });
                }

                if (string.IsNullOrWhiteSpace(createUserRequest.UserName) ||
                    string.IsNullOrWhiteSpace(createUserRequest.Email) ||
                    string.IsNullOrWhiteSpace(createUserRequest.Password))
                {
                    return BadRequest("UserName, Email, and Password are required fields.");
                }

                var result = await _sender.Send(new CreateUserCommand(createUserRequest));
                if (result == null)
                {
                    return ApiError<AuthResponse>("User registration failed.", new List<string> { "Unknown error occurred." });
                }

                if (!result.IsSuccess)
                {
                    return ApiError<AuthResponse>("User registration failed.", result.Errors ?? new List<string> { "Unknown error occurred." });
                }

                return ApiSuccess<AuthResponse>("User registered successfully", result);
            }
            catch (Exception ex)
            {
                return ApiServerError<AuthResponse>($"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("sign-in")]
        public async Task<IActionResult> SignIn([FromBody] SignIn signIn)
        {
            try
            {
                if (signIn == null)
                {
                    return ApiError<AuthResponse>("Request body cannot be null.", new List<string> { "Request body cannot be null." });
                }

                if (string.IsNullOrWhiteSpace(signIn.Email) || string.IsNullOrWhiteSpace(signIn.Password))
                {
                    return ApiError<AuthResponse>("Email and Password are required fields.", new List<string> { "Email and Password are required fields." });
                }

                var result = await _sender.Send(new SignInCommand(signIn));
                if (result == null)
                {
                    return ApiError<AuthResponse>("User sign-in failed.", new List<string> { "Unknown error occurred." });
                }

                if (!result.IsSuccess)
                {
                    return ApiError<AuthResponse>("User sign-in failed.", result.Errors ?? new List<string> { "Unknown error occurred." });
                }

                return ApiSuccess<AuthResponse>("User signed in successfully", result);
            }
            catch (Exception ex)
            {
                return ApiServerError<AuthResponse>($"An error occurred: {ex.Message}");
            }
        }


        [Authorize]
        [HttpGet("get-user")]
        public async Task<IActionResult> GetUser()
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return ApiUnauthorized<UserDto>("User ID not found in token.");
                }

                var user = await _sender.Send(new GetUserByIdQuery(userId));
                if (user == null)
                {
                    return ApiNotFound<object>("User not found.");
                }

                return ApiSuccess("User retrieved successfully", user);
            }
            catch (Exception ex)
            {
                return ApiServerError<UserDto>($"An error occurred: {ex.Message}");
            }
        }

        [Authorize]
        [HttpGet("get-user/{userId}")]
        public async Task<IActionResult> GetUserById([FromRoute] string userId)
        {
            try
            {
                var myUserId = GetUserId() ?? null;

                if (string.IsNullOrEmpty(userId))
                {
                    return ApiUnauthorized<UserDto>("User ID not found in token.");
                }

                var user = await _sender.Send(new GetUserByIdQuery(userId, myUserId));
                if (user == null)
                {
                    return ApiNotFound<object>("User not found.");
                }

                return ApiSuccess("User retrieved successfully", user);
            }
            catch (Exception ex)
            {
                return ApiServerError<UserDto>($"An error occurred: {ex.Message}");
            }
        }

        //[Authorize]
        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers(string q, string? userId, int page = 1, int limit = 20)
        {
            try
            {
                //var myUserId = GetUserId() ?? null;

                var result = await _sender.Send(new SearchUsersQuery(q, userId, page, limit));

                return ApiSuccess<IEnumerable<UserDto>>("Users retrieved successfully", result.Results);
            }
            catch (Exception ex)
            {
                return ApiServerError<UserDto>($"An error occurred: {ex.Message}");
            }
        }

        [Authorize]
        [HttpPut("update-user")]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserDto userDto)
        {
            try
            {
                if (userDto == null)
                {
                    return ApiError<UserDto>("Request body cannot be null.");
                }
                if (string.IsNullOrWhiteSpace(userDto.Id))
                {
                    return BadRequest("User ID is required.");
                }

                var result = await _sender.Send(new UpdateUserCommand(userDto));
                if (result == null)
                {
                    return ApiNotFound<object>("User not found.");
                }

                return ApiSuccess<UserDto>("User updated successfully", result);
            }
            catch (Exception ex)
            {
                return ApiServerError<UserDto>($"An error occurred: {ex.Message}");
            }
        }

        [Authorize]
        [HttpDelete("delete-user")]
        public async Task<IActionResult> DeleteUser()
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return ApiUnauthorized<UserDto>("User ID not found in token.");
                }

                var result = await _sender.Send(new DeleteUserCommand(userId));
                if (!result)
                {
                    return ApiNotFound<object>("User not found.");
                }

                return ApiSuccess<object>("User deleted successfully.", null);
            }
            catch (Exception ex)
            {
                return ApiServerError<UserDto>($"An error occurred: {ex.Message}");
            }
        }

        private string GetUserId()
        {
            return User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }

    }
}
