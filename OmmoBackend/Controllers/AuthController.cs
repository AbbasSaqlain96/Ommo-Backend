using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly ILogger<AuthController> _logger;

        /// <summary>
        /// Initializes a new instance of AuthController class with the specified auth service.
        /// </summary>
        public AuthController(IAuthService authService, ILogger<AuthController> logger, IUserService userService)
        {
            _authService = authService;
            _logger = logger;
            _userService = userService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            _logger.LogInformation("Login attempt for Email/Phone: {EmailOrPhone}", loginRequest.EmailOrPhone);

            // Check if the request model is valid
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid login request model for Email/Phone: {EmailOrPhone}", loginRequest.EmailOrPhone);
                return new BadRequestObjectResult(new { errorMessage = ErrorMessages.InvalidRequest });
            }

            try
            {
                // Call the authentication method to handle the logic of validating the user's credentials
                var result = await _authService.AuthenticateAsync(loginRequest);

                // If authentication fails, return a 401 Unauthorized response with an error message
                if (!result.Success)
                {
                    _logger.LogWarning("Unauthorized login attempt for Email/Phone: {EmailOrPhone}", loginRequest.EmailOrPhone);

                    // If user is not active, return a 403 Forbidden (user is not allowed to login)
                    if (result.ErrorMessage.Contains("not active"))
                    {
                        return ApiResponse.Error(result.ErrorMessage, result.StatusCode);
                    }

                    return ApiResponse.Error(result.ErrorMessage, result.StatusCode);
                }

                _logger.LogInformation("Successful login for Email/Phone: {EmailOrPhone}", loginRequest.EmailOrPhone);

                // If authentication succeeds, return a 200 OK response with the generated token
                return ApiResponse.Success(new { token = result.Data.Token, refreshToken = result.Data.RefreshToken }, "Login successful.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during login for Email/Phone: {EmailOrPhone}", loginRequest.EmailOrPhone);
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            _logger.LogInformation("Refresh token request received.");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid refresh token request.");
                //return BadRequest(new { error = "Invalid token refresh request." });
                return ApiResponse.Error("Invalid token refresh request", 400);
            }

            try
            {
                var result = await _authService.RefreshTokenAsync(request.RefreshToken);

                if (!result.Success) 
                {
                    _logger.LogWarning("Invalid or expired refresh token provided.");
                    return ApiResponse.Error(result.ErrorMessage, result.StatusCode);
                }

                _logger.LogInformation("Refresh token issued successfully.");
                //return Ok(new { token = result.Data.Token, refreshToken = result.Data.RefreshToken });
                return ApiResponse.Success(new { token = result.Data.Token, refreshToken = result.Data.RefreshToken }, "Token refreshed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the refresh token request.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpPost("logout")]
        [AllowAnonymous]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            _logger.LogInformation("Logout request received.");

            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                _logger.LogWarning("Logout failed: Refresh token is missing.");
                //return BadRequest(new { error = "Refresh token is required." });
                return ApiResponse.Error("Refresh token is required.", 400);
            }

            try
            {
                var result = await _authService.RevokeRefreshTokenAsync(request.RefreshToken);
                if (!result.Success)
                {
                    return ApiResponse.Error(result.ErrorMessage, result.StatusCode);
                }

                _logger.LogInformation("User logged out successfully.");
                return ApiResponse.Success(null, "Logout successful.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the logout request.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpPost("reset-password/request")]
        [AllowAnonymous]
        public async Task<IActionResult> RequestResetLink([FromBody] ResetPasswordRequestDto dto)
        {
            if (!ModelState.IsValid)
                return ApiResponse.Error("Invalid request body.", 400);

            var resp = await _userService.RequestPasswordResetAsync(dto.EmailAddress);
            return resp.Success
                ? ApiResponse.Success(null, resp.Message)
                : ApiResponse.Error(resp.ErrorMessage, resp.StatusCode);
        }

        [HttpPost("reset-password/confirm")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmReset([FromBody] ResetPasswordConfirmDto dto)
        {
            if (!ModelState.IsValid)
                return ApiResponse.Error("Invalid request body.", 400);

            var resp = await _userService.ConfirmPasswordResetAsync(dto);
            return resp.Success
                ? ApiResponse.Success(null, resp.Message)
                : ApiResponse.Error(resp.ErrorMessage, resp.StatusCode); 
        }
    }
}