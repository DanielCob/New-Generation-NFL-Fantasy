using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFL_Fantasy_API.Helpers.Extensions;
using NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Interfaces.Auth;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.DTOs.Auth;

namespace NFL_Fantasy_API.LogicLayer.SqlLogic.Controllers.Auth
{
    /// <summary>
    /// Controller de autenticación y gestión de sesiones.
    /// 
    /// RESPONSABILIDAD ÚNICA: Manejo de solicitudes HTTP
    /// - Recibe requests
    /// - Extrae datos del contexto HTTP
    /// - Delega lógica de negocio al servicio
    /// - Retorna respuestas HTTP apropiadas
    /// 
    /// NO HACE:
    /// - Validaciones (manejadas por ActionFilters)
    /// - Logging de errores (manejado por ExceptionFilter)
    /// - Lógica de negocio (delegada a AuthService)
    /// 
    /// ENDPOINTS:
    /// - POST /api/auth/register - Registro de usuario
    /// - POST /api/auth/login - Inicio de sesión
    /// - POST /api/auth/logout - Cierre de sesión actual
    /// - POST /api/auth/logout-all - Cierre de todas las sesiones
    /// - POST /api/auth/request-reset - Solicitud de restablecimiento
    /// - POST /api/auth/reset-with-token - Restablecimiento con token
    /// 
    /// Feature 1.1: Registro, autenticación y gestión de sesiones
    /// </summary>
    [ApiController]
    [Route("api/auth")]
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
        /// Registra un nuevo usuario en el sistema.
        /// POST /api/auth/register
        /// </summary>
        /// <param name="dto">Datos del nuevo usuario (email, username, password)</param>
        /// <returns>ApiResponseDTO con resultado del registro</returns>
        /// <response code="200">Usuario registrado exitosamente</response>
        /// <response code="400">Datos inválidos o email ya existe</response>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponseDTO>> Register([FromBody] RegisterUserDTO dto)
        {
            // Obtener datos del contexto HTTP
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            // Delegar al servicio
            var result = await _authService.RegisterAsync(dto, sourceIp, userAgent);

            // Log de éxito (los errores los maneja el ExceptionFilter)
            if (result.Success)
            {
                _logger.LogInformation(
                    "User registered successfully: {Email} from {IP}",
                    dto.Email,
                    sourceIp
                );
            }

            // Retornar respuesta apropiada
            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        /// <summary>
        /// Inicia sesión de usuario.
        /// POST /api/auth/login
        /// </summary>
        /// <param name="dto">Credenciales (email y password)</param>
        /// <returns>ApiResponseDTO con SessionID para usar como Bearer token</returns>
        /// <response code="200">Login exitoso, retorna SessionID</response>
        /// <response code="401">Credenciales inválidas o cuenta bloqueada</response>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponseDTO>> Login([FromBody] LoginDTO dto)
        {
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _authService.LoginAsync(dto, sourceIp, userAgent);

            if (result.Success)
            {
                _logger.LogInformation(
                    "User logged in successfully: {Email} from {IP}",
                    dto.Email,
                    sourceIp
                );
            }

            return result.Success
                ? Ok(result)
                : Unauthorized(result);
        }

        /// <summary>
        /// Cierra la sesión actual del usuario.
        /// POST /api/auth/logout
        /// </summary>
        /// <returns>ApiResponseDTO confirmando cierre de sesión</returns>
        /// <response code="200">Sesión cerrada exitosamente</response>
        /// <response code="401">No autenticado</response>
        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult<ApiResponseDTO>> Logout()
        {
            var sessionId = this.SessionId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _authService.LogoutAsync(sessionId, sourceIp, userAgent);

            _logger.LogInformation(
                "User {UserID} logged out successfully from {IP}",
                this.UserId(),
                sourceIp
            );

            return Ok(result);
        }

        /// <summary>
        /// Cierra todas las sesiones activas del usuario en todos los dispositivos.
        /// POST /api/auth/logout-all
        /// </summary>
        /// <returns>ApiResponseDTO confirmando cierre de todas las sesiones</returns>
        /// <response code="200">Todas las sesiones cerradas exitosamente</response>
        /// <response code="401">No autenticado</response>
        [HttpPost("logout-all")]
        [Authorize]
        public async Task<ActionResult<ApiResponseDTO>> LogoutAll()
        {
            var userId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _authService.LogoutAllAsync(userId, sourceIp, userAgent);

            _logger.LogInformation(
                "User {UserID} closed all sessions from {IP}",
                userId,
                sourceIp
            );

            return Ok(result);
        }

        /// <summary>
        /// Solicita un restablecimiento de contraseña.
        /// POST /api/auth/request-reset
        /// </summary>
        /// <param name="dto">Email del usuario</param>
        /// <returns>ApiResponseDTO confirmando envío de email (siempre exitoso por seguridad)</returns>
        /// <response code="200">Email enviado si el usuario existe</response>
        /// <remarks>
        /// Por seguridad, siempre retorna 200 OK aunque el email no exista.
        /// Esto previene enumeración de usuarios.
        /// </remarks>
        [HttpPost("request-reset")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponseDTO>> RequestPasswordReset([FromBody] RequestPasswordResetDTO dto)
        {
            var sourceIp = this.ClientIp();

            var result = await _authService.RequestPasswordResetAsync(dto, sourceIp);

            _logger.LogInformation(
                "Password reset requested for {Email} from {IP}",
                dto.Email,
                sourceIp
            );

            // Siempre retornar OK por seguridad (no revelar si el email existe)
            return Ok(result);
        }

        /// <summary>
        /// Restablece contraseña usando un token válido.
        /// POST /api/auth/reset-with-token
        /// </summary>
        /// <param name="dto">Token y nueva contraseña</param>
        /// <returns>ApiResponseDTO confirmando restablecimiento</returns>
        /// <response code="200">Contraseña restablecida exitosamente</response>
        /// <response code="400">Token inválido, expirado o contraseña débil</response>
        [HttpPost("reset-with-token")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponseDTO>> ResetPasswordWithToken([FromBody] ResetPasswordWithTokenDTO dto)
        {
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            var result = await _authService.ResetPasswordWithTokenAsync(dto, sourceIp, userAgent);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Password reset successfully with token from {IP}",
                    sourceIp
                );
            }

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }
    }
}