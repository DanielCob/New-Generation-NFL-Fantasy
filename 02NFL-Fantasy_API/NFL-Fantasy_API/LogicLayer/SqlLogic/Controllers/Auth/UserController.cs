using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFL_Fantasy_API.Helpers.Extensions;
using NFL_Fantasy_API.LogicLayer.SqlLogic.Services.Interfaces.Auth;
using NFL_Fantasy_API.Models.DTOs;
using NFL_Fantasy_API.Models.DTOs.Auth;

namespace NFL_Fantasy_API.LogicLayer.SqlLogic.Controllers.Auth
{
    /// <summary>
    /// Controller de gestión de perfiles de usuario.
    /// 
    /// RESPONSABILIDAD ÚNICA: Manejo de solicitudes HTTP para perfiles
    /// - Consultar perfil completo y header
    /// - Ver sesiones activas
    /// - Actualizar datos del perfil
    /// 
    /// SEGURIDAD:
    /// - Todos los endpoints requieren autenticación
    /// - Los usuarios solo pueden ver/editar su propio perfil
    /// - Las sesiones mostradas son solo del usuario actual
    /// 
    /// ENDPOINTS:
    /// - GET /api/user/profile - Perfil completo del usuario
    /// - GET /api/user/header - Información básica del header
    /// - GET /api/user/sessions - Sesiones activas del usuario
    /// - PUT /api/user/profile - Actualizar perfil propio
    /// 
    /// Feature 1.1: Gestión de perfiles de usuarios
    /// </summary>
    [ApiController]
    [Route("api/user")]
    [Authorize] // Todos los endpoints requieren autenticación
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
        /// Obtiene el perfil completo del usuario autenticado.
        /// GET /api/user/profile
        /// </summary>
        /// <returns>Perfil con datos personales, ligas como comisionado y equipos</returns>
        /// <response code="200">Perfil obtenido exitosamente</response>
        /// <response code="404">Usuario no encontrado (caso extremo)</response>
        /// <response code="401">No autenticado</response>
        /// <remarks>
        /// INCLUYE:
        /// - Datos personales (username, email, etc.)
        /// - Ligas donde es comisionado
        /// - Equipos que posee en diferentes ligas
        /// - Estadísticas generales (opcional)
        /// </remarks>
        [HttpGet("profile")]
        public async Task<ActionResult<ApiResponseDTO>> GetProfile()
        {
            var userId = this.UserId();

            var profile = await _userService.GetUserProfileAsync(userId);

            if (profile == null)
            {
                return NotFound(ApiResponseDTO.ErrorResponse("Perfil no encontrado."));
            }

            return Ok(ApiResponseDTO.SuccessResponse(
                "Perfil obtenido exitosamente.",
                profile
            ));
        }

        /// <summary>
        /// Obtiene información básica del encabezado del perfil.
        /// GET /api/user/header
        /// </summary>
        /// <returns>Datos ligeros para mostrar en header/navbar</returns>
        /// <response code="200">Header obtenido exitosamente</response>
        /// <response code="404">Usuario no encontrado (caso extremo)</response>
        /// <response code="401">No autenticado</response>
        /// <remarks>
        /// VERSIÓN LIGERA del perfil, útil para:
        /// - Mostrar info básica en navbar
        /// - Reducir payload en requests frecuentes
        /// - Mejorar performance de UI
        /// 
        /// INCLUYE típicamente:
        /// - Username
        /// - Avatar (si existe)
        /// - Rol del sistema
        /// - Notificaciones pendientes (opcional)
        /// </remarks>
        [HttpGet("header")]
        public async Task<ActionResult<ApiResponseDTO>> GetHeader()
        {
            var userId = this.UserId();

            var header = await _userService.GetUserHeaderAsync(userId);

            if (header == null)
            {
                return NotFound(ApiResponseDTO.ErrorResponse("Usuario no encontrado."));
            }

            return Ok(ApiResponseDTO.SuccessResponse(
                "Header obtenido exitosamente.",
                header
            ));
        }

        /// <summary>
        /// Obtiene todas las sesiones activas del usuario.
        /// GET /api/user/sessions
        /// </summary>
        /// <returns>Lista de sesiones activas con detalles de dispositivo/ubicación</returns>
        /// <response code="200">Sesiones obtenidas exitosamente</response>
        /// <response code="401">No autenticado</response>
        /// <remarks>
        /// ÚTIL PARA:
        /// - Ver desde qué dispositivos está conectado
        /// - Detectar sesiones sospechosas
        /// - Gestionar seguridad de la cuenta
        /// 
        /// INFORMACIÓN DE CADA SESIÓN:
        /// - Fecha de inicio
        /// - Última actividad
        /// - IP y ubicación aproximada
        /// - Dispositivo/navegador
        /// - Si es la sesión actual
        /// 
        /// El usuario puede cerrar sesiones específicas usando el endpoint logout correspondiente.
        /// </remarks>
        [HttpGet("sessions")]
        public async Task<ActionResult<ApiResponseDTO>> GetActiveSessions()
        {
            var userId = this.UserId();

            var sessions = await _userService.GetActiveSessionsAsync(userId);

            return Ok(ApiResponseDTO.SuccessResponse(
                "Sesiones activas obtenidas exitosamente.",
                sessions
            ));
        }

        /// <summary>
        /// Actualiza el perfil del usuario autenticado.
        /// PUT /api/user/profile
        /// </summary>
        /// <param name="dto">Datos a actualizar (username, bio, avatar, etc.)</param>
        /// <returns>Confirmación de actualización exitosa</returns>
        /// <response code="200">Perfil actualizado exitosamente</response>
        /// <response code="400">Datos inválidos o username ya existe</response>
        /// <response code="401">No autenticado</response>
        /// <remarks>
        /// CAMPOS EDITABLES:
        /// - Username (si no está en uso)
        /// - FirstName, LastName
        /// - Bio / Descripción
        /// - Avatar URL
        /// - Preferencias de notificaciones
        /// 
        /// CAMPOS NO EDITABLES (protegidos):
        /// - Email (requiere verificación separada)
        /// - UserID (inmutable)
        /// - CreatedAt (histórico)
        /// - AccountStatus (solo admin)
        /// - SystemRole (solo admin)
        /// - Password (usar endpoint específico)
        /// 
        /// SEGURIDAD:
        /// - El usuario solo puede editar su propio perfil
        /// - Se registra auditoría del cambio
        /// </remarks>
        [HttpPut("profile")]
        public async Task<ActionResult<ApiResponseDTO>> UpdateProfile([FromBody] UpdateUserProfileDTO dto)
        {
            var userId = this.UserId();
            var sourceIp = this.ClientIp();
            var userAgent = this.UserAgent();

            // El actor y el target son el mismo (usuario editando su propio perfil)
            var result = await _userService.UpdateProfileAsync(
                actorUserId: userId,
                targetUserId: userId,
                dto: dto,
                sourceIp: sourceIp,
                userAgent: userAgent
            );

            if (result.Success)
            {
                _logger.LogInformation(
                    "User {UserID} updated profile successfully from {IP}",
                    userId,
                    sourceIp
                );
            }

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        /// <summary>
        /// Obtiene todos los usuarios activos del sistema.
        /// GET /api/user/active
        /// </summary>
        /// <returns>Lista de usuarios activos</returns>
        /// <response code="200">Usuarios obtenidos exitosamente</response>
        /// <response code="403">No eres ADMIN</response>
        /// <remarks>
        /// Para reportes administrativos.
        /// Solo retorna usuarios con AccountStatus = Active.
        /// 
        /// TODO: Implementar verificación de rol ADMIN
        /// Por ahora permitido a cualquier usuario autenticado.
        /// </remarks>
        [HttpGet("active")]
        // [Authorize(Policy = "AdminOnly")] // Descomentar cuando se implemente
        public async Task<ActionResult<ApiResponseDTO>> GetActiveUsers()
        {
            var users = await _userService.GetActiveUsersAsync();

            return Ok(ApiResponseDTO.SuccessResponse(
                "Usuarios activos obtenidos exitosamente.",
                users
            ));
        }
    }
}