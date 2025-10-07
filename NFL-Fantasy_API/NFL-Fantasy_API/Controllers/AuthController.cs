// Controllers/AuthController.cs
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace NFL_Fantasy_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// User login endpoint
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>Login response with session token</returns>
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDTO>> Login([FromBody] LoginRequestDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _authService.LoginAsync(request);

                if (!response.Success)
                {
                    return Unauthorized(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponseDTO
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// User logout endpoint
        /// </summary>
        /// <returns>Logout confirmation</returns>
        [HttpPost("logout")]
        public async Task<ActionResult<ApiResponseDTO>> Logout()
        {
            try
            {
                // Get session token from middleware
                if (!HttpContext.Items.TryGetValue("SessionToken", out var tokenObj) ||
                    tokenObj is not Guid sessionToken)
                {
                    return Unauthorized(new ApiResponseDTO
                    {
                        Success = false,
                        Message = "No valid session token found"
                    });
                }

                var response = await _authService.LogoutAsync(sessionToken);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponseDTO
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Change password endpoint for authenticated users
        /// </summary>
        /// <param name="request">Password change request</param>
        /// <returns>Password change confirmation</returns>
        [HttpPost("change-password")]
        public async Task<ActionResult<ApiResponseDTO>> ChangePassword([FromBody] ChangePasswordRequestDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Get user ID from middleware
                if (!HttpContext.Items.TryGetValue("UserId", out var userIdObj) ||
                    userIdObj is not int userId)
                {
                    return Unauthorized(new ApiResponseDTO
                    {
                        Success = false,
                        Message = "User ID not found in session"
                    });
                }

                var response = await _authService.ChangePasswordAsync(userId, request);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponseDTO
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Reset password endpoint for administrators
        /// </summary>
        /// <param name="request">Password reset request</param>
        /// <returns>Password reset confirmation</returns>
        [HttpPost("reset-password")]
        public async Task<ActionResult<ApiResponseDTO>> ResetPassword([FromBody] ResetPasswordRequestDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Get admin user ID from middleware
                if (!HttpContext.Items.TryGetValue("UserId", out var userIdObj) ||
                    userIdObj is not int adminUserId)
                {
                    return Unauthorized(new ApiResponseDTO
                    {
                        Success = false,
                        Message = "Admin user ID not found in session"
                    });
                }

                // Verify admin role
                if (!HttpContext.Items.TryGetValue("UserType", out var userTypeObj) ||
                    userTypeObj?.ToString() != "ADMIN")
                {
                    return Forbid("Only administrators can reset passwords");
                }

                var response = await _authService.ResetPasswordByAdminAsync(adminUserId, request);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponseDTO
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Get current user information
        /// </summary>
        /// <returns>Current user session information</returns>
        [HttpGet("me")]
        public ActionResult<object> GetCurrentUser()
        {
            try
            {
                var userId = HttpContext.Items["UserId"];
                var userType = HttpContext.Items["UserType"];

                return Ok(new
                {
                    UserId = userId,
                    UserType = userType,
                    Message = "Current user session information"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponseDTO
                {
                    Success = false,
                    Message = $"Internal server error: {ex.Message}"
                });
            }
        }
    }
}