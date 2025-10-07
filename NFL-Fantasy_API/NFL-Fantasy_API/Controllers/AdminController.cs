// Controllers/AdminController.cs
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace NFL_Fantasy_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        #region Update Users
        /// <summary>
        /// Update a client (Admin only)
        /// </summary>
        /// <param name="id">Client ID</param>
        /// <param name="request">Update data</param>
        /// <returns>Update result</returns>
        [HttpPut("clients/{id}")]
        public async Task<ActionResult<ApiResponseDTO>> UpdateClient(int id, [FromBody] UpdateClientDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Verify admin role (should be handled by middleware, but double-check)
                if (!HttpContext.Items.TryGetValue("UserType", out var userTypeObj) ||
                    userTypeObj?.ToString() != "ADMIN")
                {
                    return Forbid("Only administrators can update users");
                }

                var response = await _adminService.UpdateClientAsync(id, request);

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
        /// Update an engineer (Admin only)
        /// </summary>
        /// <param name="id">Engineer ID</param>
        /// <param name="request">Update data</param>
        /// <returns>Update result</returns>
        [HttpPut("engineers/{id}")]
        public async Task<ActionResult<ApiResponseDTO>> UpdateEngineer(int id, [FromBody] UpdateEngineerDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Verify admin role
                if (!HttpContext.Items.TryGetValue("UserType", out var userTypeObj) ||
                    userTypeObj?.ToString() != "ADMIN")
                {
                    return Forbid("Only administrators can update users");
                }

                var response = await _adminService.UpdateEngineerAsync(id, request);

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
        /// Update an administrator (Admin only)
        /// </summary>
        /// <param name="id">Administrator ID</param>
        /// <param name="request">Update data</param>
        /// <returns>Update result</returns>
        [HttpPut("administrators/{id}")]
        public async Task<ActionResult<ApiResponseDTO>> UpdateAdministrator(int id, [FromBody] UpdateAdministratorDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Verify admin role
                if (!HttpContext.Items.TryGetValue("UserType", out var userTypeObj) ||
                    userTypeObj?.ToString() != "ADMIN")
                {
                    return Forbid("Only administrators can update users");
                }

                var response = await _adminService.UpdateAdministratorAsync(id, request);

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
        #endregion

        #region Delete Users
        /// <summary>
        /// Delete a client permanently (Admin only)
        /// </summary>
        /// <param name="id">Client ID</param>
        /// <returns>Deletion result</returns>
        [HttpDelete("clients/{id}")]
        public async Task<ActionResult<ApiResponseDTO>> DeleteClient(int id)
        {
            try
            {
                // Verify admin role
                if (!HttpContext.Items.TryGetValue("UserType", out var userTypeObj) ||
                    userTypeObj?.ToString() != "ADMIN")
                {
                    return Forbid("Only administrators can delete users");
                }

                var response = await _adminService.DeleteClientAsync(id);

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
        /// Delete an engineer permanently (Admin only)
        /// </summary>
        /// <param name="id">Engineer ID</param>
        /// <returns>Deletion result</returns>
        [HttpDelete("engineers/{id}")]
        public async Task<ActionResult<ApiResponseDTO>> DeleteEngineer(int id)
        {
            try
            {
                // Verify admin role
                if (!HttpContext.Items.TryGetValue("UserType", out var userTypeObj) ||
                    userTypeObj?.ToString() != "ADMIN")
                {
                    return Forbid("Only administrators can delete users");
                }

                var response = await _adminService.DeleteEngineerAsync(id);

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
        /// Delete an administrator permanently (Admin only)
        /// </summary>
        /// <param name="id">Administrator ID</param>
        /// <returns>Deletion result</returns>
        [HttpDelete("administrators/{id}")]
        public async Task<ActionResult<ApiResponseDTO>> DeleteAdministrator(int id)
        {
            try
            {
                // Verify admin role
                if (!HttpContext.Items.TryGetValue("UserType", out var userTypeObj) ||
                    userTypeObj?.ToString() != "ADMIN")
                {
                    return Forbid("Only administrators can delete users");
                }

                // Get current admin user ID
                if (!HttpContext.Items.TryGetValue("UserId", out var currentUserIdObj) ||
                    currentUserIdObj is not int currentUserId)
                {
                    return Unauthorized("Cannot identify current user");
                }

                // Prevent self-deletion
                if (currentUserId == id)
                {
                    return BadRequest(new ApiResponseDTO
                    {
                        Success = false,
                        Message = "Cannot delete your own administrator account"
                    });
                }

                var response = await _adminService.DeleteAdministratorAsync(id);

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
        #endregion

        #region Utility Operations
        /// <summary>
        /// Clean expired session tokens (Admin only)
        /// </summary>
        /// <returns>Cleanup result</returns>
        [HttpPost("clean-expired-tokens")]
        public async Task<ActionResult<ApiResponseDTO>> CleanExpiredTokens()
        {
            try
            {
                // Verify admin role
                if (!HttpContext.Items.TryGetValue("UserType", out var userTypeObj) ||
                    userTypeObj?.ToString() != "ADMIN")
                {
                    return Forbid("Only administrators can perform maintenance operations");
                }

                var response = await _adminService.CleanExpiredTokensAsync();

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
        /// Synchronize IsActive status with session tokens (Admin only)
        /// </summary>
        /// <returns>Synchronization result</returns>
        [HttpPost("sync-active-status")]
        public async Task<ActionResult<ApiResponseDTO>> SyncActiveStatus()
        {
            try
            {
                // Verify admin role
                if (!HttpContext.Items.TryGetValue("UserType", out var userTypeObj) ||
                    userTypeObj?.ToString() != "ADMIN")
                {
                    return Forbid("Only administrators can perform maintenance operations");
                }

                var response = await _adminService.SyncActiveStatusAsync();

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
        #endregion

        #region Admin Statistics
        /// <summary>
        /// Get system statistics (Admin only)
        /// </summary>
        /// <returns>System statistics</returns>
        [HttpGet("statistics")]
        public async Task<ActionResult> GetSystemStatistics()
        {
            try
            {
                // Verify admin role
                if (!HttpContext.Items.TryGetValue("UserType", out var userTypeObj) ||
                    userTypeObj?.ToString() != "ADMIN")
                {
                    return Forbid("Only administrators can view system statistics");
                }

                // This is a placeholder for system statistics
                // You can implement actual statistics gathering here
                var stats = new
                {
                    Message = "System statistics endpoint",
                    Timestamp = DateTime.UtcNow,
                    Note = "Implement actual statistics collection as needed"
                };

                return Ok(stats);
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
        #endregion
    }
}