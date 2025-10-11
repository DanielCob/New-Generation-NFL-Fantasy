using Microsoft.AspNetCore.Mvc;
using NFL_Fantasy_API.Extensions;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Services.Interfaces;

namespace NFL_Fantasy_API.Controllers
{
    /// <summary>
    /// Controller de autenticación y gestión de sesiones
    /// Endpoints: Register, Login, Logout, LogoutAll, RequestReset, ResetWithToken
    /// Feature 1.1: Registro, autenticación y gestión de sesiones
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Registra un nuevo usuario en el sistema
        /// POST /api/auth/register
        /// Feature 1.1 - Registro de usuario
        /// Público (no requiere autenticación)
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<ApiResponseDTO>> Register([FromBody] RegisterUserDTO dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDTO.ErrorResponse(string.Join(" ", errors)));
            }

            try
            {
                var sourceIp = HttpContext.GetClientIpAddress();
                var userAgent = HttpContext.GetUserAgent();

                var result = await _authService.RegisterAsync(dto, sourceIp, userAgent);

                if (result.Success)
                {
                    _logger.LogInformation("User registered successfully: {Email} from {IP}", dto.Email, sourceIp);
                    return Ok(result);
                }

                _logger.LogWarning("Registration failed for {Email}: {Message}", dto.Email, result.Message);
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for {Email}", dto.Email);
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error interno del servidor."));
            }
        }

        /// <summary>
        /// Inicia sesión de usuario
        /// POST /api/auth/login
        /// Feature 1.1 - Inicio de sesión
        /// Público (no requiere autenticación)
        /// Retorna SessionID para usar como Bearer token
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<ApiResponseDTO>> Login([FromBody] LoginDTO dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDTO.ErrorResponse(string.Join(" ", errors)));
            }

            try
            {
                var sourceIp = HttpContext.GetClientIpAddress();
                var userAgent = HttpContext.GetUserAgent();
                var result = await _authService.LoginAsync(dto, sourceIp, userAgent);

                if (result.Success)
                {
                    _logger.LogInformation("User logged in successfully: {Email} from {IP}", dto.Email, sourceIp);
                    return Ok(result);
                }

                _logger.LogWarning("Login failed for {Email}: {Message}", dto.Email, result.Message);
                return Unauthorized(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for {Email}", dto.Email);
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error interno del servidor."));
            }
        }

        /// <summary>
        /// Cierra la sesión actual
        /// POST /api/auth/logout
        /// Feature 1.1 - Cierre de sesión
        /// Requiere autenticación (Bearer token)
        /// </summary>
        [HttpPost("logout")]
        public async Task<ActionResult<ApiResponseDTO>> Logout()
        {
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));
            }

            try
            {
                var sessionId = HttpContext.GetSessionId();
                var sourceIp = HttpContext.GetClientIpAddress();
                var userAgent = HttpContext.GetUserAgent();
                var result = await _authService.LogoutAsync(sessionId, sourceIp, userAgent);

                _logger.LogInformation("User {UserID} logged out successfully from {IP}",
                    HttpContext.GetUserId(), sourceIp);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout for user {UserID}", HttpContext.GetUserId());
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al cerrar sesión."));
            }
        }

        /// <summary>
        /// Cierra todas las sesiones activas del usuario en todos los dispositivos
        /// POST /api/auth/logout-all
        /// Feature 1.1 - Cierre de sesión global
        /// Requiere autenticación (Bearer token)
        /// </summary>
        [HttpPost("logout-all")]
        public async Task<ActionResult<ApiResponseDTO>> LogoutAll()
        {
            if (!HttpContext.IsAuthenticated())
            {
                return Unauthorized(ApiResponseDTO.ErrorResponse("No autenticado."));
            }

            try
            {
                var userId = HttpContext.GetUserId();
                var sourceIp = HttpContext.GetClientIpAddress();
                var userAgent = HttpContext.GetUserAgent();
                var result = await _authService.LogoutAllAsync(userId, sourceIp, userAgent);

                _logger.LogInformation("User {UserID} closed all sessions from {IP}", userId, sourceIp);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout-all for user {UserID}", HttpContext.GetUserId());
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al cerrar sesiones."));
            }
        }

        /// <summary>
        /// Solicita restablecimiento de contraseña
        /// POST /api/auth/request-reset
        /// Feature 1.1 - Desbloqueo de cuenta bloqueada
        /// Público (no requiere autenticación)
        /// </summary>
        [HttpPost("request-reset")]
        public async Task<ActionResult<ApiResponseDTO>> RequestPasswordReset([FromBody] RequestPasswordResetDTO dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDTO.ErrorResponse(string.Join(" ", errors)));
            }

            try
            {
                var sourceIp = HttpContext.GetClientIpAddress();
                var result = await _authService.RequestPasswordResetAsync(dto, sourceIp);

                _logger.LogInformation("Password reset requested for {Email} from {IP}", dto.Email, sourceIp);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset request for {Email}", dto.Email);
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al solicitar restablecimiento."));
            }
        }

        /// <summary>
        /// Restablece contraseña usando token válido
        /// POST /api/auth/reset-with-token
        /// Feature 1.1 - Desbloqueo de cuenta bloqueada
        /// Público (no requiere autenticación, usa token)
        /// </summary>
        [HttpPost("reset-with-token")]
        public async Task<ActionResult<ApiResponseDTO>> ResetPasswordWithToken([FromBody] ResetPasswordWithTokenDTO dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                return BadRequest(ApiResponseDTO.ErrorResponse(string.Join(" ", errors)));
            }

            try
            {
                var sourceIp = HttpContext.GetClientIpAddress();
                var userAgent = HttpContext.GetUserAgent();
                var result = await _authService.ResetPasswordWithTokenAsync(dto, sourceIp, userAgent);

                if (result.Success)
                {
                    _logger.LogInformation("Password reset successfully with token from {IP}", sourceIp);
                    return Ok(result);
                }

                _logger.LogWarning("Password reset failed: {Message}", result.Message);
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset with token");
                return StatusCode(500, ApiResponseDTO.ErrorResponse("Error al restablecer contraseña."));
            }
        }
    }
}