using Microsoft.AspNetCore.Mvc;
using NFL_Fantasy_API.Extensions;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Services.Interfaces;

namespace NFL_Fantasy_API.Controllers
{
    /// <summary>
    /// Controller de gestión de perfiles de usuario
    /// Endpoints: GetProfile, GetHeader, GetSessions, UpdateProfile
    /// Feature 1.1: Gestión de perfiles de usuarios
    /// Todos los endpoints requieren autenticación
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene el perfil completo del usuario autenticado
        /// GET /api/user/profile
        /// Feature 1.1 - Ver perfil de usuario
        /// Retorna: datos del usuario + ligas como comisionado + equipos
        /// </summary>
        [HttpGet("profile")]
        public async Task<ActionResult> GetProfile()
        {
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));
            }

            try
            {
                var userId = HttpContext.GetUserId();
                var profile = await _userService.GetUserProfileAsync(userId);

                if (profile == null)
                {
                    return NotFound(ApiResponseDTO.ErrorResponse("Perfil no encontrado."));
                }

                return Ok(ApiResponseDTO.SuccessResponse("Perfil obtenido exitosamente.", profile));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profile for user {UserID}", HttpContext.GetUserId());
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al obtener perfil."));
            }
        }

        /// <summary>
        /// Obtiene información básica del encabezado del perfil
        /// GET /api/user/header
        /// Vista ligera del perfil
        /// </summary>
        [HttpGet("header")]
        public async Task<ActionResult> GetHeader()
        {
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));
            }

            try
            {
                var userId = HttpContext.GetUserId();
                var header = await _userService.GetUserHeaderAsync(userId);

                if (header == null)
                {
                    return NotFound(ApiResponseDTO.ErrorResponse("Usuario no encontrado."));
                }

                return Ok(ApiResponseDTO.SuccessResponse("Header obtenido exitosamente.", header));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting header for user {UserID}", HttpContext.GetUserId());
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al obtener header."));
            }
        }

        /// <summary>
        /// Obtiene todas las sesiones activas del usuario
        /// GET /api/user/sessions
        /// Feature 1.1 - Ver sesiones activas
        /// Útil para ver desde qué dispositivos está conectado
        /// </summary>
        [HttpGet("sessions")]
        public async Task<ActionResult> GetActiveSessions()
        {
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));
            }

            try
            {
                var userId = HttpContext.GetUserId();
                var sessions = await _userService.GetActiveSessionsAsync(userId);

                return Ok(ApiResponseDTO.SuccessResponse("Sesiones activas obtenidas.", sessions));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sessions for user {UserID}", HttpContext.GetUserId());
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al obtener sesiones."));
            }
        }

        /// <summary>
        /// Actualiza el perfil del usuario autenticado
        /// PUT /api/user/profile
        /// Feature 1.1 - Gestión de perfil de usuario
        /// IMPORTANTE: No permite editar Email, UserID, CreatedAt, AccountStatus, Role
        /// </summary>
        [HttpPut("profile")]
        public async Task<ActionResult<ApiResponseDTO>> UpdateProfile([FromBody] UpdateUserProfileDTO dto)
        {
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDTO.ErrorResponse(string.Join(" ", errors)));
            }

            try
            {
                var userId = HttpContext.GetUserId();

                // El actor y el target son el mismo (el usuario solo puede editar su propio perfil)
                var result = await _userService.UpdateProfileAsync(userId, userId, dto);

                if (result.Success)
                {
                    _logger.LogInformation("User {UserID} updated profile successfully", userId);
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserID}", HttpContext.GetUserId());
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al actualizar perfil."));
            }
        }
    }
}