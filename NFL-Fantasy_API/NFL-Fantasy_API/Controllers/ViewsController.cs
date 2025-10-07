// Controllers/ViewsController.cs
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.ViewModels;
using NFL_Fantasy_API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace NFL_Fantasy_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ViewsController : ControllerBase
    {
        private readonly IUserService _userService;

        public ViewsController(IUserService userService)
        {
            _userService = userService;
        }

        #region Active Users Views
        /// <summary>
        /// Get all active clients (Admin only)
        /// </summary>
        /// <returns>List of active clients with session tokens</returns>
        [HttpGet("active-clients")]
        public async Task<ActionResult<IEnumerable<ClientViewModel>>> GetActiveClients()
        {
            try
            {
                // Verify admin role
                if (!HttpContext.Items.TryGetValue("UserType", out var userTypeObj) ||
                    userTypeObj?.ToString() != "ADMIN")
                {
                    return Forbid("Only administrators can view user reports");
                }

                var activeClients = await _userService.GetActiveClientsAsync();
                return Ok(activeClients);
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
        /// Get all active engineers (Admin only)
        /// </summary>
        /// <returns>List of active engineers with session tokens</returns>
        [HttpGet("active-engineers")]
        public async Task<ActionResult<IEnumerable<EngineerViewModel>>> GetActiveEngineers()
        {
            try
            {
                // Verify admin role
                if (!HttpContext.Items.TryGetValue("UserType", out var userTypeObj) ||
                    userTypeObj?.ToString() != "ADMIN")
                {
                    return Forbid("Only administrators can view user reports");
                }

                var activeEngineers = await _userService.GetActiveEngineersAsync();
                return Ok(activeEngineers);
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
        /// Get all active administrators (Admin only)
        /// </summary>
        /// <returns>List of active administrators with session tokens</returns>
        [HttpGet("active-administrators")]
        public async Task<ActionResult<IEnumerable<AdministratorViewModel>>> GetActiveAdministrators()
        {
            try
            {
                // Verify admin role
                if (!HttpContext.Items.TryGetValue("UserType", out var userTypeObj) ||
                    userTypeObj?.ToString() != "ADMIN")
                {
                    return Forbid("Only administrators can view user reports");
                }

                var activeAdmins = await _userService.GetActiveAdministratorsAsync();
                return Ok(activeAdmins);
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

        #region All Users Views
        /// <summary>
        /// Get all clients (Admin only)
        /// </summary>
        /// <returns>List of all clients with session status</returns>
        [HttpGet("all-clients")]
        public async Task<ActionResult<IEnumerable<ClientViewModel>>> GetAllClients()
        {
            try
            {
                // Verify admin role
                if (!HttpContext.Items.TryGetValue("UserType", out var userTypeObj) ||
                    userTypeObj?.ToString() != "ADMIN")
                {
                    return Forbid("Only administrators can view user reports");
                }

                var allClients = await _userService.GetAllClientsAsync();
                return Ok(allClients);
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
        /// Get all engineers (Admin only)
        /// </summary>
        /// <returns>List of all engineers with session status</returns>
        [HttpGet("all-engineers")]
        public async Task<ActionResult<IEnumerable<EngineerViewModel>>> GetAllEngineers()
        {
            try
            {
                // Verify admin role
                if (!HttpContext.Items.TryGetValue("UserType", out var userTypeObj) ||
                    userTypeObj?.ToString() != "ADMIN")
                {
                    return Forbid("Only administrators can view user reports");
                }

                var allEngineers = await _userService.GetAllEngineersAsync();
                return Ok(allEngineers);
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
        /// Get all administrators (Admin only)
        /// </summary>
        /// <returns>List of all administrators with session status</returns>
        [HttpGet("all-administrators")]
        public async Task<ActionResult<IEnumerable<AdministratorViewModel>>> GetAllAdministrators()
        {
            try
            {
                // Verify admin role
                if (!HttpContext.Items.TryGetValue("UserType", out var userTypeObj) ||
                    userTypeObj?.ToString() != "ADMIN")
                {
                    return Forbid("Only administrators can view user reports");
                }

                var allAdmins = await _userService.GetAllAdministratorsAsync();
                return Ok(allAdmins);
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

        #region Summary Views
        /// <summary>
        /// Get user summary statistics (Admin only)
        /// </summary>
        /// <returns>Summary of user counts by type and status</returns>
        [HttpGet("summary")]
        public async Task<ActionResult> GetUserSummary()
        {
            try
            {
                // Verify admin role
                if (!HttpContext.Items.TryGetValue("UserType", out var userTypeObj) ||
                    userTypeObj?.ToString() != "ADMIN")
                {
                    return Forbid("Only administrators can view user reports");
                }

                // Get data for all user types
                var allClients = await _userService.GetAllClientsAsync();
                var allEngineers = await _userService.GetAllEngineersAsync();
                var allAdmins = await _userService.GetAllAdministratorsAsync();

                // Calculate summary statistics
                var summary = new
                {
                    Clients = new
                    {
                        Total = allClients.Count(),
                        Active = allClients.Count(c => c.HasActiveSession),
                        Inactive = allClients.Count(c => !c.HasActiveSession)
                    },
                    Engineers = new
                    {
                        Total = allEngineers.Count(),
                        Active = allEngineers.Count(e => e.HasActiveSession),
                        Inactive = allEngineers.Count(e => !e.HasActiveSession)
                    },
                    Administrators = new
                    {
                        Total = allAdmins.Count(),
                        Active = allAdmins.Count(a => a.HasActiveSession),
                        Inactive = allAdmins.Count(a => !a.HasActiveSession)
                    },
                    Overall = new
                    {
                        TotalUsers = allClients.Count() + allEngineers.Count() + allAdmins.Count(),
                        TotalActiveUsers = allClients.Count(c => c.HasActiveSession) +
                                         allEngineers.Count(e => e.HasActiveSession) +
                                         allAdmins.Count(a => a.HasActiveSession),
                        GeneratedAt = DateTime.UtcNow
                    }
                };

                return Ok(summary);
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
        /// Get recent user activity (Admin only)
        /// </summary>
        /// <param name="days">Number of days to look back (default: 7)</param>
        /// <returns>Recent user activity summary</returns>
        [HttpGet("recent-activity")]
        public async Task<ActionResult> GetRecentActivity([FromQuery] int days = 7)
        {
            try
            {
                // Verify admin role
                if (!HttpContext.Items.TryGetValue("UserType", out var userTypeObj) ||
                    userTypeObj?.ToString() != "ADMIN")
                {
                    return Forbid("Only administrators can view user reports");
                }

                // Validate days parameter
                if (days < 1 || days > 365)
                {
                    return BadRequest(new ApiResponseDTO
                    {
                        Success = false,
                        Message = "Days parameter must be between 1 and 365"
                    });
                }

                var cutoffDate = DateTime.Now.AddDays(-days);

                // Get all users data
                var allClients = await _userService.GetAllClientsAsync();
                var allEngineers = await _userService.GetAllEngineersAsync();
                var allAdmins = await _userService.GetAllAdministratorsAsync();

                // Filter recent users
                var recentClients = allClients.Where(c => c.CreatedAt >= cutoffDate).Count();
                var recentEngineers = allEngineers.Where(e => e.CreatedAt >= cutoffDate).Count();
                var recentAdmins = allAdmins.Where(a => a.CreatedAt >= cutoffDate).Count();

                var recentActivity = new
                {
                    Period = $"Last {days} days",
                    StartDate = cutoffDate,
                    EndDate = DateTime.Now,
                    NewRegistrations = new
                    {
                        Clients = recentClients,
                        Engineers = recentEngineers,
                        Administrators = recentAdmins,
                        Total = recentClients + recentEngineers + recentAdmins
                    },
                    CurrentlyActive = new
                    {
                        Clients = allClients.Count(c => c.HasActiveSession),
                        Engineers = allEngineers.Count(e => e.HasActiveSession),
                        Administrators = allAdmins.Count(a => a.HasActiveSession)
                    }
                };

                return Ok(recentActivity);
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