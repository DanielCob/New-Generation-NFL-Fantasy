// Controllers/UserController.cs
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace NFL_Fantasy_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        #region Create Users
        /// <summary>
        /// Create a new client
        /// </summary>
        /// <param name="request">Client creation data</param>
        /// <returns>Creation result</returns>
        [HttpPost("clients")]
        public async Task<ActionResult<ApiResponseDTO>> CreateClient([FromBody] CreateClientDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _userService.CreateClientAsync(request);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return CreatedAtAction(nameof(GetClientById), new { id = 0 }, response);
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
        /// Create a new engineer
        /// </summary>
        /// <param name="request">Engineer creation data</param>
        /// <returns>Creation result</returns>
        [HttpPost("engineers")]
        public async Task<ActionResult<ApiResponseDTO>> CreateEngineer([FromBody] CreateEngineerDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _userService.CreateEngineerAsync(request);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return CreatedAtAction(nameof(GetEngineerById), new { id = 0 }, response);
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
        /// Create a new administrator
        /// </summary>
        /// <param name="request">Administrator creation data</param>
        /// <returns>Creation result</returns>
        [HttpPost("administrators")]
        public async Task<ActionResult<ApiResponseDTO>> CreateAdministrator([FromBody] CreateAdministratorDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _userService.CreateAdministratorAsync(request);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return CreatedAtAction(nameof(GetAdministratorById), new { id = 0 }, response);
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

        #region Get Users by ID
        /// <summary>
        /// Get client by ID
        /// </summary>
        /// <param name="id">Client ID</param>
        /// <returns>Client information</returns>
        [HttpGet("clients/{id}")]
        public async Task<ActionResult<UserResponseDTO>> GetClientById(int id)
        {
            try
            {
                var client = await _userService.GetClientByIdAsync(id);

                if (client == null)
                {
                    return NotFound(new ApiResponseDTO
                    {
                        Success = false,
                        Message = "Client not found"
                    });
                }

                return Ok(client);
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
        /// Get engineer by ID
        /// </summary>
        /// <param name="id">Engineer ID</param>
        /// <returns>Engineer information</returns>
        [HttpGet("engineers/{id}")]
        public async Task<ActionResult<EngineerResponseDTO>> GetEngineerById(int id)
        {
            try
            {
                var engineer = await _userService.GetEngineerByIdAsync(id);

                if (engineer == null)
                {
                    return NotFound(new ApiResponseDTO
                    {
                        Success = false,
                        Message = "Engineer not found"
                    });
                }

                return Ok(engineer);
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
        /// Get administrator by ID
        /// </summary>
        /// <param name="id">Administrator ID</param>
        /// <returns>Administrator information</returns>
        [HttpGet("administrators/{id}")]
        public async Task<ActionResult<AdministratorResponseDTO>> GetAdministratorById(int id)
        {
            try
            {
                var administrator = await _userService.GetAdministratorByIdAsync(id);

                if (administrator == null)
                {
                    return NotFound(new ApiResponseDTO
                    {
                        Success = false,
                        Message = "Administrator not found"
                    });
                }

                return Ok(administrator);
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

        #region Get Current User Information
        /// <summary>
        /// Get current user's complete information
        /// </summary>
        /// <returns>Current user's detailed information</returns>
        [HttpGet("me/details")]
        public async Task<ActionResult> GetCurrentUserDetails()
        {
            try
            {
                // Get user information from middleware
                if (!HttpContext.Items.TryGetValue("UserId", out var userIdObj) ||
                    userIdObj is not int userId)
                {
                    return Unauthorized(new ApiResponseDTO
                    {
                        Success = false,
                        Message = "User ID not found in session"
                    });
                }

                if (!HttpContext.Items.TryGetValue("UserType", out var userTypeObj) ||
                    userTypeObj?.ToString() is not string userType)
                {
                    return Unauthorized(new ApiResponseDTO
                    {
                        Success = false,
                        Message = "User type not found in session"
                    });
                }

                // Get detailed information based on user type
                object? userDetails = userType switch
                {
                    "CLIENT" => await _userService.GetClientByIdAsync(userId),
                    "ENGINEER" => await _userService.GetEngineerByIdAsync(userId),
                    "ADMIN" => await _userService.GetAdministratorByIdAsync(userId),
                    _ => null
                };

                if (userDetails == null)
                {
                    return NotFound(new ApiResponseDTO
                    {
                        Success = false,
                        Message = "User details not found"
                    });
                }

                return Ok(userDetails);
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