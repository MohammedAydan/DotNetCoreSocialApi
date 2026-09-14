using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Social.Core.Interfaces;
using Social.Application.Features.Users.Commands;
using Social.Application.Features.Users.DTOs;
using Social.Core.Entities;

namespace Social.API.Controllers.Dashboard
{
    [Route("api/dashboard/[controller]")]
    [ApiController]
    public class UserController : BaseController
    {
        private readonly ISender _sender;
        private readonly ICacheService _cache;

        public UserController(ISender sender, ICacheService cache)
        {
            _sender = sender;
            _cache = cache;
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

                // check if user is admin
                if (!result.IsAdmin())
                {
                    return ApiUnauthorized<AuthResponse>("User does not have admin privileges.");
                }

                return ApiSuccess<AuthResponse>("User signed in successfully", result);
            }
            catch (Exception ex)
            {
                return ApiServerError<AuthResponse>($"An error occurred: {ex.Message}");
            }
        }

        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest refreshTokenRequest)
        {
            try
            {
                if (refreshTokenRequest == null || string.IsNullOrWhiteSpace(refreshTokenRequest.RefreshToken))
                {
                    return ApiError<AuthResponse>("Refresh token is required.", new List<string> { "Refresh token is required." });
                }
                var result = await _sender.Send(new RefreshTokenCommand(refreshTokenRequest.RefreshToken));
                if (result == null)
                {
                    return ApiError<AuthResponse>("Token refresh failed.", new List<string> { "Unknown error occurred." });
                }
                if (!result.IsSuccess)
                {
                    return ApiError<AuthResponse>("Token refresh failed.", result.Errors ?? new List<string> { "Unknown error occurred." });
                }

                return ApiSuccess<AuthResponse>("Token refreshed successfully", result);
            }
            catch (Exception ex)
            {
                return ApiServerError<AuthResponse>($"An error occurred: {ex.Message}");
            }
        }

    }
}